using NLog;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marathon
{
    public class Network
    {
        public Network(Runner runner)
        {
            Runner = runner;
        }

        public Runner Runner { get; private set; }

        private static Logger Log = LogManager.GetCurrentClassLogger();

        public virtual async Task<Models.BuildInfo> RequestBuild()
        {
            Log.Trace("Requesting Build");

            var request = new RestRequest("/api/v1/builds/register.json", Method.POST);
            request.JsonSerializer = new Helpers.RestSharpJsonNetSerializer();
            request.AddJsonBody(new Models.BuildRequest()
            {
                Token = Runner.Configuration.Get("token")
            });

            var response = await Runner.Client.ExecuteTaskAsync<Models.BuildInfo>(request);

            if (response.ErrorException != null)
            {
                Log.WarnException("Failed to connect to GitLab CI server", response.ErrorException);
                return null;
            }
            switch (response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                case System.Net.HttpStatusCode.Created:
                    Log.Debug(response.Content);
                    return response.Data;
                case System.Net.HttpStatusCode.NotFound:
                    Log.Trace("No builds available for this runner");
                    return null;
                default:
                    return null;
            }
        }

        public virtual async Task<NetworkResponse> UpdateBuild(Models.BuildInfo buildInfo, BuildState state, string output)
        {
            Log.Trace("Updating build #{0} - {1} ({2})", buildInfo.ID, buildInfo.ProjectName, state);

            var request = new RestRequest("/api/v1/builds/{id}.json", Method.PUT);
            request.JsonSerializer = new Helpers.RestSharpJsonNetSerializer();
            request.AddUrlSegment("id", buildInfo.ID.ToString());
            request.AddJsonBody(new Models.BuildStatus()
            {
                Token = Runner.Configuration.Get("token"),
                State = state.ToString(),
                Trace = output
            });

            var response = await Runner.Client.ExecuteTaskAsync(request);
            if (response.ErrorException != null)
            {
                Log.WarnException("Failed to connect to GitLab CI server", response.ErrorException);
                return NetworkResponse.Failure;
            }
            switch(response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    Log.Trace("Build #{0} updated", buildInfo.ID);
                    return NetworkResponse.Success;
                case System.Net.HttpStatusCode.NotFound:
                    Log.Trace("Build #{0} aborted", buildInfo.ID);
                    return NetworkResponse.Aborted;
                default:
                    Log.Trace("Failed to update build - {0}", response.StatusCode);
                    return NetworkResponse.Failure;
            }

        }
        public async Task<Models.RegistrationResponse> RegisterRunner(string token, string description, params string[] tags)
        {
            Log.Trace("Registering new runner {0}", description);

            var request = new RestRequest("/api/v1/runners/register.json", Method.POST);
            request.JsonSerializer = new Helpers.RestSharpJsonNetSerializer();
            request.AddJsonBody(new Models.RegistrationRequest()
            {
                Token = token,
                Description = description,
                TagList = tags.Select(x => x.Trim()).Where(x => x.Length > 0).Aggregate((left, right) => left + ", " + right)
            });

            var response = await Runner.Client.ExecuteTaskAsync<Models.RegistrationResponse>(request);
            if(response.ErrorException != null)
            {
                Log.WarnException("Failed to connect to GitLab CI server", response.ErrorException);
                return null;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Created)
            {
                Log.Trace("Runner registered with ID {0}", response.Data.ID);
                return response.Data;
            }

            Log.Trace("Failed to register runner: {0}", response.Content);
            return null;
        }
    }

    public enum NetworkResponse
    {
        Success,
        Aborted,
        Forbidden,
        Failure
    }
}
