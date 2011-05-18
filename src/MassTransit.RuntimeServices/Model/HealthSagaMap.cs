// Copyright 2007-2011 Chris Patterson, Dru Sellers, Travis Smith, et. al.
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
namespace MassTransit.RuntimeServices.Model
{
	using FluentNHibernate.Mapping;
	using NHibernateIntegration;
	using Services.HealthMonitoring.Server;
	using Util;

	[UsedImplicitly]
	public class HealthSagaMap :
		ClassMap<HealthSaga>
	{
		public HealthSagaMap()
		{
			Id(x => x.CorrelationId)
				.GeneratedBy.Assigned();

			Map(x => x.CurrentState)
				.Access.ReadOnlyPropertyThroughCamelCaseField(Prefix.Underscore)
				.CustomType<StateMachineUserType>();

			Map(x => x.ControlUri)
				.CustomType<UriUserType>();
			Map(x => x.DataUri)
				.CustomType<UriUserType>();

			Map(x => x.LastHeartbeat);
			Map(x => x.HeartbeatIntervalInSeconds);
		}
	}
}