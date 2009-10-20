// Copyright 2007-2008 The Apache Software Foundation.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace WebLoadTest
{
	using System;
	using System.Diagnostics;
	using System.Web;
	using Actors;
	using Magnum;
	using MassTransit;
	using MassTransit.Actors;
	using StructureMap.Pipeline;

	public class ActorHttpAsyncHandler<T> :
		IHttpAsyncHandler
		where T : StateDrivenActor<T>
	{
		private readonly IServiceBus _bus;
		private readonly IActorRepository<T> _actorRepository;
		private T _actor;

		public ActorHttpAsyncHandler(IServiceBus bus, IActorRepository<T> actorRepository)
		{
			_bus = bus;
			_actorRepository = actorRepository;
		}

		public void ProcessRequest(HttpContext context)
		{
			throw new InvalidOperationException("This should not be called since we are an asynchronous handler");
		}

		public bool IsReusable
		{
			get { return true; }
		}

		public IAsyncResult BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData)
		{
			Guid transactionId = CombGuid.Generate();

			_actor = (T) Activator.CreateInstance(typeof (T), new[] {transactionId, context, cb, extraData});

			_actorRepository.Add(_actor);

			_bus.Endpoint.Send(new InitiateStockQuoteRequestImpl {RequestId = transactionId, Symbol = "AAPL"});

			return _actor;
		}

		public void EndProcessRequest(IAsyncResult result)
		{
			_actorRepository.Remove(_actor);

			Trace.WriteLine("Size of repository: " + _actorRepository.Count());
		}
	}
}