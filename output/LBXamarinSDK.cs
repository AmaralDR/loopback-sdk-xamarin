/** Generated at 11/28/2017 19:14:33 */

/**
 *** Hardcoded Models ***
 */

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RestSharp.Portable;
//using LBXamarinSDK;
//using LBXamarinSDK.LBRepo;
//using System.Net.Http;
using System.Threading;
//using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Converters;
using RestSharp.Portable.Deserializers;
using System.Diagnostics;
using Prism.Mvvm;
using Prism.Navigation;
using System.Collections.ObjectModel;


namespace LBXamarinSDK
{
	// Gateway: Communication with Server API
	public class Gateway
    {
        private static Uri BASE_URL = new Uri("http://127.0.0.1:3000/api/");
        private static Uri BASE_SERVER = new Uri("http://127.0.0.1:3000/");
        private static RestSharp.Portable.HttpClient.RestClient _client = new RestSharp.Portable.HttpClient.RestClient { BaseUrl = BASE_URL };
        private static string _accessToken = null;
		private static AccessToken _user = null;
		private static bool _debugMode = false;
        private static CancellationTokenSource _cts = new CancellationTokenSource();
		private static int _timeout = 6000;
		private static bool initFlag = false;

		// Custom deserializer to handle timezones formats sent from loopback
		private class CustomConverter : IDeserializer
        {
            private static readonly JsonSerializerSettings SerializerSettings;
            static CustomConverter ()
            {
                SerializerSettings = new JsonSerializerSettings
                {
                    DateTimeZoneHandling = DateTimeZoneHandling.Local,
                    Converters = new List<JsonConverter> { new IsoDateTimeConverter() },
                    NullValueHandling = NullValueHandling.Ignore
                };
            }

            public T Deserialize<T>(IRestResponse response)
            {
                var type = typeof(T);
                var rawBytes = response.RawBytes;
                return (T)JsonConvert.DeserializeObject (UTF8Encoding.UTF8.GetString (rawBytes, 0, rawBytes.Length), type, SerializerSettings);
            }

            public System.Net.Http.Headers.MediaTypeHeaderValue ContentType { get; set; }
        }

		// Allow Console WriteLines to debug communication with server
		public static void SetDebugMode(bool isDebugMode)
		{
			_debugMode = isDebugMode;
			if(_debugMode)
			{
				Debug.WriteLine("******************************");
				Debug.WriteLine("** SDK Gateway Debug Mode.  **");
				Debug.WriteLine("******************************\n");
			}
		}


		// Debug mode getter
		public static bool GetDebugMode()
		{
			return _debugMode;
		}
		
		/*** Cancellation-Token methods, define a timeout for a server request ***/
		private static void ResetCancellationToken()
		{
			_cts = new CancellationTokenSource();
            _cts.CancelAfter(_timeout);
		}

        public static void SetTimeout(int timeoutMilliseconds = 6000)
        {
			_timeout = timeoutMilliseconds;
			ResetCancellationToken();
        }
		/* *** */

	    public static string GetBaseUrlServer()
        {
            return BASE_SERVER.ToString();
        }
        public static void SetServerBaseURL(string baseUrl = "127.0.0.1:3000", string rotaAPI = "api")
        {
            BASE_SERVER = new Uri(String.Format("http://{0}/", baseUrl));
            BASE_URL = new Uri(String.Format("http://{0}/{1}/", baseUrl, rotaAPI));
            _client.BaseUrl = BASE_URL;
        }
        // Define server Base Url for API requests. Example: "http://127.0.0.1:3000/api/"
        public static void SetServerBaseURL(Uri baseUrl)
        {
            BASE_SERVER = baseUrl;
            BASE_URL = baseUrl;
            _client.BaseUrl = baseUrl;
        }

		// Sets an access token to be added as an authorization in all future server requests
        public static void SetAccessToken(AccessToken accessToken)
        {
            if (accessToken != null)
                _accessToken = accessToken.id;
				_user = accessToken;
        }

		// Get the access token ID currently being used by the gateway
		public static string GetAccessTokenId()
        {
            return _accessToken;
        }
		public static String GetUser()
        {
            return _user.userId;
        }

		// Performs a request to determine if connected to server
        public static async Task<bool> isConnected(int timeoutMilliseconds = 6000)
		{
			SetTimeout(timeoutMilliseconds);
			_cts.Token.ThrowIfCancellationRequested();
			try
			{
				var request = new RestRequest ("/", Method.GET);
				var response = await _client.Execute<JObject>(request, _cts.Token).ConfigureAwait(false);
				if (response != null)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			catch(Exception e)
			{
				if (_debugMode)
                    Debug.WriteLine("-------- >> DEBUG: Error: " + e.Message + " >>");	 
				return false;
			}

			return false;
		}

		// Resets the authorization token
        public static void ResetAccessToken()
        {
            _accessToken = null;
        }
        
		// Makes a request through restSharp to server
		public static async Task<T> MakeRequest<T>(RestRequest request)
        {
            ResetCancellationToken();
            _cts.Token.ThrowIfCancellationRequested();
            _client.IgnoreResponseStatusCode = true;

            if (!initFlag)
            {
                _client.ReplaceHandler(typeof(JsonDeserializer), new CustomConverter());
                initFlag = true;
            }
				try
			{

            var response = await _client.Execute<JRaw>(request, _cts.Token).ConfigureAwait(false);
            var responseData = response.Data != null ? response.Data.ToString() : "";
            
            if (!response.IsSuccess)
            {
			
                if(_debugMode)
                    Debug.WriteLine("-------- >> DEBUG: Error performing request, status code " + (int)response.StatusCode + ", Payload: " + responseData);
                throw new RestException(responseData, (int)response.StatusCode);
            }

            return JsonConvert.DeserializeObject<T>(responseData);
        }
		            catch (Exception ex)
            {
                throw;
            }
        }


		// Parses a server request then makes it through MakeRequest
        public static async Task<T> PerformRequest<T>(string APIUrl, string json, string method = "POST", IDictionary<string, string> queryStrings = null)
		{
			RestRequest request = null;
            request = new RestRequest(APIUrl, (Method)Enum.Parse(typeof(Method), method));

            if(_debugMode)
                Debug.WriteLine("-------- >> DEBUG: Performing " + method + " request at URL: '" + _client.BuildUri(request) + "', Json: " + (string.IsNullOrEmpty(json) ? "EMPTY" : json));

			// Add query parameters to the request
            if (queryStrings != null)
            {
                foreach (var query in queryStrings)
                {
                    if (!string.IsNullOrEmpty(query.Value))
                    {
                        request.AddParameter(query.Key, query.Value, ParameterType.QueryString);
                    }
                }
            }

			// Add authorization token to the request
            if (!String.IsNullOrEmpty(_accessToken))
            {
                request.AddHeader("Authorization", _accessToken);
            }

			// Add body parameters to the request
			if ((method == "POST" || method == "PUT") && json != "")
            {
				request.AddHeader("ContentType", "application/json");
				request.AddParameter ("application/json", JObject.Parse(json), ParameterType.RequestBody);
			}

			// Make the request, return response
			var response = await MakeRequest<T>(request).ConfigureAwait(false);
			return response;
		}

        // T is the expected return type, U is the input type. E.g. U is Car, T is Car
        public static async Task<T> PerformPostRequest<U, T>(U objToPost, string APIUrl, IDictionary<string, string> queryStrings = null)
        {
            var res = await PerformRequest<T>(APIUrl, JsonConvert.SerializeObject(objToPost), "POST", queryStrings).ConfigureAwait(false);
            return res;
        }

        // T is the expected return type. For example "Car" for get or "Car[]" for get all cars
        public static async Task<T> PerformGetRequest<T>(string APIUrl, IDictionary<string, string> queryStrings = null)
        {	
            var res = await PerformRequest<T>(APIUrl, "", "GET", queryStrings).ConfigureAwait(false);
            return res;
        }

        // T is the expected return type, U is the input type. E.g. U is Car, T is Car
        public static async Task<T> PerformPutRequest<U, T>(U objToPut, string APIUrl, IDictionary<string, string> queryStrings = null)
        {
            var res = await PerformRequest<T>(APIUrl, JsonConvert.SerializeObject(objToPut), "PUT", queryStrings).ConfigureAwait(false);
            return res;
        }
    }

	
	    // Base model for all LBXamarinSDK Models with MVVM
    public abstract class LBModel : BindableBase, INavigationAware, IDestructible
    {

        public virtual String getID()
        {
            return "";
        }
        public virtual void OnNavigatedFrom(NavigationParameters parameters)
        {

        }

        public virtual void OnNavigatedTo(NavigationParameters parameters)
        {

        }

        public virtual void OnNavigatingTo(NavigationParameters parameters)
        {

        }

        public virtual void Destroy()
        {

        }
    }
	
	// Allow conversion between the return type of login methods into AccessToken, e.g. "AccessToken myAccessToken = await Users.login(someCredentials);
	// TODO: Add this jobject->class implicit conversion as a templated function for all classes inheriting from model
	public partial class AccessToken : LBModel
    {
        public static implicit operator AccessToken(JObject jObj)
        {
            if (jObj == null)
            {
                return null;
            }
            return JsonConvert.DeserializeObject<AccessToken>(jObj.ToString());
        }
    }

	// GeoPoint primitive loopback type
	public class GeoPoint : LBModel
	{
		// Must be leq than 90: TODO: Add attributes or setter limitations
		[JsonProperty("lat", NullValueHandling = NullValueHandling.Ignore)]
		public double Latitude { get; set; }

		[JsonProperty("lng", NullValueHandling = NullValueHandling.Ignore)]
		public double Longitude { get; set; }
	}

	// Exception class, thrown on bad REST requests
	class RestException : Exception
    {
		public int StatusCode { get; private set; }

		private static int parseStatusCode(string responseString)
		{
            Regex statusCodeRegex = new Regex(@"[0-9]{3}");
            if (statusCodeRegex.IsMatch(responseString))
            {
                Match match = statusCodeRegex.Match(responseString);
				return Int32.Parse(match.Groups[0].Value);
			}
			else
			{
				return 0;
			}
		}

		public RestException(string responseString) : base(responseString)
		{
			StatusCode = parseStatusCode(responseString);
		}

		public RestException(string responseString, int StatusCode) : base(responseString)
		{
            this.StatusCode = StatusCode;
		}
    }
}
/**
 *** Dynamic Repositories ***
 */

namespace LBXamarinSDK
{
    namespace LBRepo
    {
		/* CRUD Interface holds the basic CRUD operations for all models.
		   In turn, all repositories will inherit from this.
		*/
        public abstract class CRUDInterface<T> where T : LBModel
        {
			private static readonly Dictionary<string, string> APIDictionary = new Dictionary<string, string>
            {
				{"user/create", "Users"}, 
				{"user/exists", "Users/:id/exists"}, 
				{"user/findbyid", "Users/:id"}, 
				{"user/find", "Users"}, 
				{"user/findone", "Users/findOne"}, 
				{"user/updateall", "Users/update"}, 
				{"user/deletebyid", "Users/:id"}, 
				{"user/count", "Users/count"}, 
				{"accesstoken/create", "AccessTokens"}, 
				{"accesstoken/exists", "AccessTokens/:id/exists"}, 
				{"accesstoken/findbyid", "AccessTokens/:id"}, 
				{"accesstoken/find", "AccessTokens"}, 
				{"accesstoken/findone", "AccessTokens/findOne"}, 
				{"accesstoken/updateall", "AccessTokens/update"}, 
				{"accesstoken/deletebyid", "AccessTokens/:id"}, 
				{"accesstoken/count", "AccessTokens/count"}, 
				{"usercredential/create", "UserCredentials"}, 
				{"usercredential/exists", "UserCredentials/:id/exists"}, 
				{"usercredential/findbyid", "UserCredentials/:id"}, 
				{"usercredential/find", "UserCredentials"}, 
				{"usercredential/findone", "UserCredentials/findOne"}, 
				{"usercredential/updateall", "UserCredentials/update"}, 
				{"usercredential/deletebyid", "UserCredentials/:id"}, 
				{"usercredential/count", "UserCredentials/count"}, 
				{"useridentity/create", "UserIdentities"}, 
				{"useridentity/exists", "UserIdentities/:id/exists"}, 
				{"useridentity/findbyid", "UserIdentities/:id"}, 
				{"useridentity/find", "UserIdentities"}, 
				{"useridentity/findone", "UserIdentities/findOne"}, 
				{"useridentity/updateall", "UserIdentities/update"}, 
				{"useridentity/deletebyid", "UserIdentities/:id"}, 
				{"useridentity/count", "UserIdentities/count"}, 
				{"acl/create", "ACLs"}, 
				{"acl/exists", "ACLs/:id/exists"}, 
				{"acl/findbyid", "ACLs/:id"}, 
				{"acl/find", "ACLs"}, 
				{"acl/findone", "ACLs/findOne"}, 
				{"acl/updateall", "ACLs/update"}, 
				{"acl/deletebyid", "ACLs/:id"}, 
				{"acl/count", "ACLs/count"}, 
				{"rolemapping/create", "RoleMappings"}, 
				{"rolemapping/exists", "RoleMappings/:id/exists"}, 
				{"rolemapping/findbyid", "RoleMappings/:id"}, 
				{"rolemapping/find", "RoleMappings"}, 
				{"rolemapping/findone", "RoleMappings/findOne"}, 
				{"rolemapping/updateall", "RoleMappings/update"}, 
				{"rolemapping/deletebyid", "RoleMappings/:id"}, 
				{"rolemapping/count", "RoleMappings/count"}, 
				{"role/create", "Roles"}, 
				{"role/exists", "Roles/:id/exists"}, 
				{"role/findbyid", "Roles/:id"}, 
				{"role/find", "Roles"}, 
				{"role/findone", "Roles/findOne"}, 
				{"role/updateall", "Roles/update"}, 
				{"role/deletebyid", "Roles/:id"}, 
				{"role/count", "Roles/count"}, 
				{"enumerado/create", "enumerados"}, 
				{"enumerado/exists", "enumerados/:id/exists"}, 
				{"enumerado/findbyid", "enumerados/:id"}, 
				{"enumerado/find", "enumerados"}, 
				{"enumerado/findone", "enumerados/findOne"}, 
				{"enumerado/updateall", "enumerados/update"}, 
				{"enumerado/deletebyid", "enumerados/:id"}, 
				{"enumerado/count", "enumerados/count"}, 
				{"parametro/create", "parametros"}, 
				{"parametro/exists", "parametros/:id/exists"}, 
				{"parametro/findbyid", "parametros/:id"}, 
				{"parametro/find", "parametros"}, 
				{"parametro/findone", "parametros/findOne"}, 
				{"parametro/updateall", "parametros/update"}, 
				{"parametro/deletebyid", "parametros/:id"}, 
				{"parametro/count", "parametros/count"}, 
				{"job/create", "jobs"}, 
				{"job/exists", "jobs/:id/exists"}, 
				{"job/findbyid", "jobs/:id"}, 
				{"job/find", "jobs"}, 
				{"job/findone", "jobs/findOne"}, 
				{"job/updateall", "jobs/update"}, 
				{"job/deletebyid", "jobs/:id"}, 
				{"job/count", "jobs/count"}, 
				{"pais/create", "pais"}, 
				{"pais/exists", "pais/:id/exists"}, 
				{"pais/findbyid", "pais/:id"}, 
				{"pais/find", "pais"}, 
				{"pais/findone", "pais/findOne"}, 
				{"pais/updateall", "pais/update"}, 
				{"pais/deletebyid", "pais/:id"}, 
				{"pais/count", "pais/count"}, 
				{"textopadrao/create", "textoPadraos"}, 
				{"textopadrao/exists", "textoPadraos/:id/exists"}, 
				{"textopadrao/findbyid", "textoPadraos/:id"}, 
				{"textopadrao/find", "textoPadraos"}, 
				{"textopadrao/findone", "textoPadraos/findOne"}, 
				{"textopadrao/updateall", "textoPadraos/update"}, 
				{"textopadrao/deletebyid", "textoPadraos/:id"}, 
				{"textopadrao/count", "textoPadraos/count"}, 
				{"pessoa/create", "pessoas"}, 
				{"pessoa/exists", "pessoas/:id/exists"}, 
				{"pessoa/findbyid", "pessoas/:id"}, 
				{"pessoa/find", "pessoas"}, 
				{"pessoa/findone", "pessoas/findOne"}, 
				{"pessoa/updateall", "pessoas/update"}, 
				{"pessoa/deletebyid", "pessoas/:id"}, 
				{"pessoa/count", "pessoas/count"}, 
				{"perfilacesso/create", "perfilAcessos"}, 
				{"perfilacesso/exists", "perfilAcessos/:id/exists"}, 
				{"perfilacesso/findbyid", "perfilAcessos/:id"}, 
				{"perfilacesso/find", "perfilAcessos"}, 
				{"perfilacesso/findone", "perfilAcessos/findOne"}, 
				{"perfilacesso/updateall", "perfilAcessos/update"}, 
				{"perfilacesso/deletebyid", "perfilAcessos/:id"}, 
				{"perfilacesso/count", "perfilAcessos/count"}, 
				{"rota/create", "rota"}, 
				{"rota/exists", "rota/:id/exists"}, 
				{"rota/findbyid", "rota/:id"}, 
				{"rota/find", "rota"}, 
				{"rota/findone", "rota/findOne"}, 
				{"rota/updateall", "rota/update"}, 
				{"rota/deletebyid", "rota/:id"}, 
				{"rota/count", "rota/count"}, 
				{"periodicidade/create", "periodicidades"}, 
				{"periodicidade/exists", "periodicidades/:id/exists"}, 
				{"periodicidade/findbyid", "periodicidades/:id"}, 
				{"periodicidade/find", "periodicidades"}, 
				{"periodicidade/findone", "periodicidades/findOne"}, 
				{"periodicidade/updateall", "periodicidades/update"}, 
				{"periodicidade/deletebyid", "periodicidades/:id"}, 
				{"periodicidade/count", "periodicidades/count"}, 
				{"tiposervico/create", "tipoServicos"}, 
				{"tiposervico/exists", "tipoServicos/:id/exists"}, 
				{"tiposervico/findbyid", "tipoServicos/:id"}, 
				{"tiposervico/find", "tipoServicos"}, 
				{"tiposervico/findone", "tipoServicos/findOne"}, 
				{"tiposervico/updateall", "tipoServicos/update"}, 
				{"tiposervico/deletebyid", "tipoServicos/:id"}, 
				{"tiposervico/count", "tipoServicos/count"}, 
				{"produto/create", "produtos"}, 
				{"produto/exists", "produtos/:id/exists"}, 
				{"produto/findbyid", "produtos/:id"}, 
				{"produto/find", "produtos"}, 
				{"produto/findone", "produtos/findOne"}, 
				{"produto/updateall", "produtos/update"}, 
				{"produto/deletebyid", "produtos/:id"}, 
				{"produto/count", "produtos/count"}, 
				{"dica/create", "dicas"}, 
				{"dica/exists", "dicas/:id/exists"}, 
				{"dica/findbyid", "dicas/:id"}, 
				{"dica/find", "dicas"}, 
				{"dica/findone", "dicas/findOne"}, 
				{"dica/updateall", "dicas/update"}, 
				{"dica/deletebyid", "dicas/:id"}, 
				{"dica/count", "dicas/count"}, 
				{"aplicativo/create", "aplicativos"}, 
				{"aplicativo/exists", "aplicativos/:id/exists"}, 
				{"aplicativo/findbyid", "aplicativos/:id"}, 
				{"aplicativo/find", "aplicativos"}, 
				{"aplicativo/findone", "aplicativos/findOne"}, 
				{"aplicativo/updateall", "aplicativos/update"}, 
				{"aplicativo/deletebyid", "aplicativos/:id"}, 
				{"aplicativo/count", "aplicativos/count"}, 
				{"examepreparo/create", "examePreparos"}, 
				{"examepreparo/exists", "examePreparos/:id/exists"}, 
				{"examepreparo/findbyid", "examePreparos/:id"}, 
				{"examepreparo/find", "examePreparos"}, 
				{"examepreparo/findone", "examePreparos/findOne"}, 
				{"examepreparo/updateall", "examePreparos/update"}, 
				{"examepreparo/deletebyid", "examePreparos/:id"}, 
				{"examepreparo/count", "examePreparos/count"}, 
				{"agendamento/create", "agendamentos"}, 
				{"agendamento/exists", "agendamentos/:id/exists"}, 
				{"agendamento/findbyid", "agendamentos/:id"}, 
				{"agendamento/find", "agendamentos"}, 
				{"agendamento/findone", "agendamentos/findOne"}, 
				{"agendamento/updateall", "agendamentos/update"}, 
				{"agendamento/deletebyid", "agendamentos/:id"}, 
				{"agendamento/count", "agendamentos/count"}, 
				{"instalacao/create", "instalacaos"}, 
				{"instalacao/exists", "instalacaos/:id/exists"}, 
				{"instalacao/findbyid", "instalacaos/:id"}, 
				{"instalacao/find", "instalacaos"}, 
				{"instalacao/findone", "instalacaos/findOne"}, 
				{"instalacao/updateall", "instalacaos/update"}, 
				{"instalacao/deletebyid", "instalacaos/:id"}, 
				{"instalacao/count", "instalacaos/count"}, 
				{"notificacao/create", "notificacaos"}, 
				{"notificacao/exists", "notificacaos/:id/exists"}, 
				{"notificacao/findbyid", "notificacaos/:id"}, 
				{"notificacao/find", "notificacaos"}, 
				{"notificacao/findone", "notificacaos/findOne"}, 
				{"notificacao/updateall", "notificacaos/update"}, 
				{"notificacao/deletebyid", "notificacaos/:id"}, 
				{"notificacao/count", "notificacaos/count"}, 
				{"dispositivo/create", "dispositivos"}, 
				{"dispositivo/exists", "dispositivos/:id/exists"}, 
				{"dispositivo/findbyid", "dispositivos/:id"}, 
				{"dispositivo/find", "dispositivos"}, 
				{"dispositivo/findone", "dispositivos/findOne"}, 
				{"dispositivo/updateall", "dispositivos/update"}, 
				{"dispositivo/deletebyid", "dispositivos/:id"}, 
				{"dispositivo/count", "dispositivos/count"}, 
				{"pessoaregistro/create", "pessoaRegistros"}, 
				{"pessoaregistro/exists", "pessoaRegistros/:id/exists"}, 
				{"pessoaregistro/findbyid", "pessoaRegistros/:id"}, 
				{"pessoaregistro/find", "pessoaRegistros"}, 
				{"pessoaregistro/findone", "pessoaRegistros/findOne"}, 
				{"pessoaregistro/updateall", "pessoaRegistros/update"}, 
				{"pessoaregistro/deletebyid", "pessoaRegistros/:id"}, 
				{"pessoaregistro/count", "pessoaRegistros/count"}, 
				{"pessoaalergia/create", "pessoaAlergia"}, 
				{"pessoaalergia/exists", "pessoaAlergia/:id/exists"}, 
				{"pessoaalergia/findbyid", "pessoaAlergia/:id"}, 
				{"pessoaalergia/find", "pessoaAlergia"}, 
				{"pessoaalergia/findone", "pessoaAlergia/findOne"}, 
				{"pessoaalergia/updateall", "pessoaAlergia/update"}, 
				{"pessoaalergia/deletebyid", "pessoaAlergia/:id"}, 
				{"pessoaalergia/count", "pessoaAlergia/count"}, 
				{"pessoahistoricofamiliar/create", "pessoaHistoricoFamiliars"}, 
				{"pessoahistoricofamiliar/exists", "pessoaHistoricoFamiliars/:id/exists"}, 
				{"pessoahistoricofamiliar/findbyid", "pessoaHistoricoFamiliars/:id"}, 
				{"pessoahistoricofamiliar/find", "pessoaHistoricoFamiliars"}, 
				{"pessoahistoricofamiliar/findone", "pessoaHistoricoFamiliars/findOne"}, 
				{"pessoahistoricofamiliar/updateall", "pessoaHistoricoFamiliars/update"}, 
				{"pessoahistoricofamiliar/deletebyid", "pessoaHistoricoFamiliars/:id"}, 
				{"pessoahistoricofamiliar/count", "pessoaHistoricoFamiliars/count"}, 
				{"pessoaprotocolo/create", "pessoaProtocolos"}, 
				{"pessoaprotocolo/exists", "pessoaProtocolos/:id/exists"}, 
				{"pessoaprotocolo/findbyid", "pessoaProtocolos/:id"}, 
				{"pessoaprotocolo/find", "pessoaProtocolos"}, 
				{"pessoaprotocolo/findone", "pessoaProtocolos/findOne"}, 
				{"pessoaprotocolo/updateall", "pessoaProtocolos/update"}, 
				{"pessoaprotocolo/deletebyid", "pessoaProtocolos/:id"}, 
				{"pessoaprotocolo/count", "pessoaProtocolos/count"}, 
				{"protocolo/create", "protocolos"}, 
				{"protocolo/exists", "protocolos/:id/exists"}, 
				{"protocolo/findbyid", "protocolos/:id"}, 
				{"protocolo/find", "protocolos"}, 
				{"protocolo/findone", "protocolos/findOne"}, 
				{"protocolo/updateall", "protocolos/update"}, 
				{"protocolo/deletebyid", "protocolos/:id"}, 
				{"protocolo/count", "protocolos/count"}, 
				{"cadastropessoa/create", "cadastroPessoas"}, 
				{"cadastropessoa/exists", "cadastroPessoas/:id/exists"}, 
				{"cadastropessoa/findbyid", "cadastroPessoas/:id"}, 
				{"cadastropessoa/find", "cadastroPessoas"}, 
				{"cadastropessoa/findone", "cadastroPessoas/findOne"}, 
				{"cadastropessoa/updateall", "cadastroPessoas/update"}, 
				{"cadastropessoa/deletebyid", "cadastroPessoas/:id"}, 
				{"cadastropessoa/count", "cadastroPessoas/count"}, 
			};

			// Getter for API paths of CRUD methods
			protected static String getAPIPath(String crudMethodName)
            {
				Type baseType = typeof(T);
				String dictionaryKey = string.Format("{0}/{1}", baseType.Name, crudMethodName).ToLower();

				if(!APIDictionary.ContainsKey(dictionaryKey))
				{
					if(Gateway.GetDebugMode())
						Debug.WriteLine("Error - no known CRUD path for " + dictionaryKey);
					throw new Exception();
				}
				return APIDictionary[dictionaryKey];
            }

            /* All the basic CRUD: Hardcoded */

			/*
			 * Create a new instance of the model and persist it into the data source
			 */
            public static async Task<T> Create(T theModel)
            {
                String APIPath = getAPIPath("Create");
                var response = await Gateway.PerformPostRequest<T, T>(theModel, APIPath).ConfigureAwait(false);
                return response;
            }

			/*
			 * Update an existing model instance or insert a new one into the data source
			 */
            public static async Task<T> Upsert(T theModel)
            {
                String APIPath = getAPIPath("Upsert");
                var response = await Gateway.PerformPutRequest<T, T>(theModel, APIPath).ConfigureAwait(false);
                return response;
            }

			/*
			 * Check whether a model instance exists in the data source
			 */
            public static async Task<bool> Exists(string ID)
            {
                String APIPath = getAPIPath("Exists");
                APIPath = APIPath.Replace(":id", ID);
                var response = await Gateway.PerformGetRequest<object>(APIPath).ConfigureAwait(false);
                return JObject.Parse(response.ToString()).First.First.ToObject<bool>();
            }

			/*
			 * Find a model instance by id from the data source
			 */
            public static async Task<T> FindById(String ID)
            {
                String APIPath = getAPIPath("FindById");
                APIPath = APIPath.Replace(":id", ID);
                var response = await Gateway.PerformGetRequest<T>(APIPath).ConfigureAwait(false);
                return response;
            }

			/*
			 * Find all instances of the model matched by filter from the data source
			 */
            public static async Task<IList<T>> Find(string filter = "")
            {
                String APIPath = getAPIPath("Find");
                IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				queryStrings.Add("filter", filter);
                var response = await Gateway.PerformGetRequest<T[]>(APIPath, queryStrings).ConfigureAwait(false);
                return response.ToList();
            }

			/*
			 * Find first instance of the model matched by filter from the data source
			 */
            public static async Task<T> FindOne(string filter = "")
            {
                String APIPath = getAPIPath("FindOne");
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				queryStrings.Add("filter", filter);
                var response = await Gateway.PerformGetRequest<T>(APIPath, queryStrings).ConfigureAwait(false);
                return response;
            }

			/*
			 * Update instances of the model matched by where from the data source
			 */
            public static async Task UpdateAll(T updateModel, string whereFilter)
            {
				String APIPath = getAPIPath("UpdateAll");
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				queryStrings.Add("where", whereFilter);
                var response = await Gateway.PerformPostRequest<T, string>(updateModel, APIPath, queryStrings).ConfigureAwait(false);
            }

			/*
			 * Delete a model instance by id from the data source
			 */
            public static async Task DeleteById(String ID)
            {
				String APIPath = getAPIPath("DeleteById");
                APIPath = APIPath.Replace(":id", ID);
                var response = await Gateway.PerformRequest<string>(APIPath, "", "DELETE").ConfigureAwait(false);
            }

			/*
			 * Count instances of the model matched by where from the data source
			 */
            public static async Task<int> Count(string whereFilter = "")
            {
                String APIPath = getAPIPath("Count");
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				queryStrings.Add("where", whereFilter);
                var response = await Gateway.PerformGetRequest<object>(APIPath, queryStrings).ConfigureAwait(false);
                return JObject.Parse(response.ToString()).First.First.ToObject<int>();
            }

			/*
			 * Update attributes for a model instance and persist it into the data source
			 */
            public static async Task<T> UpdateById(String ID, T update)
            {
                String APIPath = getAPIPath("prototype$updateAttributes");
                APIPath = APIPath.Replace(":id", ID);
                var response = await Gateway.PerformPutRequest<T, T>(update, APIPath).ConfigureAwait(false);
                return response;
            }
        }

		// Dynamic repositories for all Dynamic models:
		public class Emails : CRUDInterface<Email>
		{
		}
		public class Users : CRUDInterface<User>
		{

			/*
			 * Find a related item by id for accessTokens.
			 */
			public static async Task<AccessToken> findByIdAccessTokens(string id, string fk)
			{
				string APIPath = "Users/:id/accessTokens/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for accessTokens.
			 */
			public static async Task destroyByIdAccessTokens(string id, string fk)
			{
				string APIPath = "Users/:id/accessTokens/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for accessTokens.
			 */
			public static async Task<AccessToken> updateByIdAccessTokens(User data, string id, string fk)
			{
				string APIPath = "Users/:id/accessTokens/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for identities.
			 */
			public static async Task<UserIdentity> findByIdIdentities(string id, string fk)
			{
				string APIPath = "Users/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for identities.
			 */
			public static async Task destroyByIdIdentities(string id, string fk)
			{
				string APIPath = "Users/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for identities.
			 */
			public static async Task<UserIdentity> updateByIdIdentities(User data, string id, string fk)
			{
				string APIPath = "Users/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for credentials.
			 */
			public static async Task<UserCredential> findByIdCredentials(string id, string fk)
			{
				string APIPath = "Users/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for credentials.
			 */
			public static async Task destroyByIdCredentials(string id, string fk)
			{
				string APIPath = "Users/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for credentials.
			 */
			public static async Task<UserCredential> updateByIdCredentials(User data, string id, string fk)
			{
				string APIPath = "Users/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries accessTokens of User.
			 */
			public static async Task<IList<AccessToken>> getAccessTokens(string id, string filter = default(string))
			{
				string APIPath = "Users/:id/accessTokens";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<AccessToken[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in accessTokens of this model.
			 */
			public static async Task<AccessToken> createAccessTokens(User data, string id)
			{
				string APIPath = "Users/:id/accessTokens";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all accessTokens of this model.
			 */
			public static async Task deleteAccessTokens(string id)
			{
				string APIPath = "Users/:id/accessTokens";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts accessTokens of User.
			 */
			public static async Task<double> countAccessTokens(string id, string where = default(string))
			{
				string APIPath = "Users/:id/accessTokens/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Queries identities of User.
			 */
			public static async Task<IList<UserIdentity>> getIdentities(string id, string filter = default(string))
			{
				string APIPath = "Users/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<UserIdentity[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in identities of this model.
			 */
			public static async Task<UserIdentity> createIdentities(User data, string id)
			{
				string APIPath = "Users/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all identities of this model.
			 */
			public static async Task deleteIdentities(string id)
			{
				string APIPath = "Users/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts identities of User.
			 */
			public static async Task<double> countIdentities(string id, string where = default(string))
			{
				string APIPath = "Users/:id/identities/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Queries credentials of User.
			 */
			public static async Task<IList<UserCredential>> getCredentials(string id, string filter = default(string))
			{
				string APIPath = "Users/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<UserCredential[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in credentials of this model.
			 */
			public static async Task<UserCredential> createCredentials(User data, string id)
			{
				string APIPath = "Users/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all credentials of this model.
			 */
			public static async Task deleteCredentials(string id)
			{
				string APIPath = "Users/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts credentials of User.
			 */
			public static async Task<double> countCredentials(string id, string where = default(string))
			{
				string APIPath = "Users/:id/credentials/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<User> patchOrCreate(User data)
			{
				string APIPath = "Users";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<User> replaceOrCreate(User data)
			{
				string APIPath = "Users/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<User> upsertWithWhere(User data, string where = default(string))
			{
				string APIPath = "Users/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<User> replaceById(User data, string id)
			{
				string APIPath = "Users/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<User> patchAttributes(User data, string id)
			{
				string APIPath = "Users/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Login a user with username/email and password.
			 */
			public static async Task<JObject> login(User credentials, string include = default(string))
			{
				string APIPath = "Users/login";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(credentials);
				queryStrings.Add("include", include != null ? include.ToString() : null);
				var response = await Gateway.PerformRequest<JObject>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Logout a user with access token.
			 */
			public static async Task logout()
			{
				string APIPath = "Users/logout";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Trigger user-s identity verification with configured verifyOptions
			 */
			public static async Task verify(string id)
			{
				string APIPath = "Users/:id/verify";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Confirm a user registration with identity verification token.
			 */
			public static async Task confirm(string uid = default(string), string token = default(string), string redirect = default(string))
			{
				string APIPath = "Users/confirm";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("uid", uid != null ? uid.ToString() : null);
				queryStrings.Add("token", token != null ? token.ToString() : null);
				queryStrings.Add("redirect", redirect != null ? redirect.ToString() : null);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Reset password for a user with email.
			 */
			public static async Task resetPassword(User options)
			{
				string APIPath = "Users/reset";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(options);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Alter password for user with temporary token.
			 */
			public static async Task changePassword(User options)
			{
				string APIPath = "Users/changePassword";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(options);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Reset user-s password via a password-reset token.
			 */
			public static async Task setPassword(string newPassword)
			{
				string APIPath = "Users/reset-password";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Fetches belongsTo relation user.
			 */
			public static async Task<User> getForAccessToken(string id, bool refresh = default(bool))
			{
				string APIPath = "AccessTokens/:id/user";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Fetches belongsTo relation user.
			 */
			public static async Task<User> getForUserCredential(string id, bool refresh = default(bool))
			{
				string APIPath = "UserCredentials/:id/user";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Fetches belongsTo relation user.
			 */
			public static async Task<User> getForUserIdentity(string id, bool refresh = default(bool))
			{
				string APIPath = "UserIdentities/:id/user";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for user.
			 */
			public static async Task<User> findByIdForpessoa(string id, string fk)
			{
				string APIPath = "pessoas/:id/user/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for user.
			 */
			public static async Task destroyByIdForpessoa(string id, string fk)
			{
				string APIPath = "pessoas/:id/user/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for user.
			 */
			public static async Task<User> updateByIdForpessoa(User data, string id, string fk)
			{
				string APIPath = "pessoas/:id/user/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Add a related item by id for user.
			 */
			public static async Task<User> linkForpessoa(string id, string fk)
			{
				string APIPath = "pessoas/:id/user/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Remove the user relation to an item by id.
			 */
			public static async Task unlinkForpessoa(string id, string fk)
			{
				string APIPath = "pessoas/:id/user/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Check the existence of user relation to an item by id.
			 */
			public static async Task<bool> existsForpessoa(string id, string fk)
			{
				string APIPath = "pessoas/:id/user/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<bool>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries user of pessoa.
			 */
			public static async Task<IList<User>> getForpessoa(string id, string filter = default(string))
			{
				string APIPath = "pessoas/:id/user";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<User[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in user of this model.
			 */
			public static async Task<User> createForpessoa(User data, string id)
			{
				string APIPath = "pessoas/:id/user";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all user of this model.
			 */
			public static async Task deleteForpessoa(string id)
			{
				string APIPath = "pessoas/:id/user";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts user of pessoa.
			 */
			public static async Task<double> countForpessoa(string id, string where = default(string))
			{
				string APIPath = "pessoas/:id/user/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}
		}
		public class AccessTokens : CRUDInterface<AccessToken>
		{

			/*
			 * Fetches belongsTo relation user.
			 */
			public static async Task<User> getUser(string id, bool refresh = default(bool))
			{
				string APIPath = "AccessTokens/:id/user";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<AccessToken> patchOrCreate(AccessToken data)
			{
				string APIPath = "AccessTokens";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<AccessToken> replaceOrCreate(AccessToken data)
			{
				string APIPath = "AccessTokens/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<AccessToken> upsertWithWhere(AccessToken data, string where = default(string))
			{
				string APIPath = "AccessTokens/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<AccessToken> replaceById(AccessToken data, string id)
			{
				string APIPath = "AccessTokens/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<AccessToken> patchAttributes(AccessToken data, string id)
			{
				string APIPath = "AccessTokens/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for accessTokens.
			 */
			public static async Task<AccessToken> findByIdForUser(string id, string fk)
			{
				string APIPath = "Users/:id/accessTokens/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for accessTokens.
			 */
			public static async Task destroyByIdForUser(string id, string fk)
			{
				string APIPath = "Users/:id/accessTokens/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for accessTokens.
			 */
			public static async Task<AccessToken> updateByIdForUser(AccessToken data, string id, string fk)
			{
				string APIPath = "Users/:id/accessTokens/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries accessTokens of User.
			 */
			public static async Task<IList<AccessToken>> getForUser(string id, string filter = default(string))
			{
				string APIPath = "Users/:id/accessTokens";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<AccessToken[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in accessTokens of this model.
			 */
			public static async Task<AccessToken> createForUser(AccessToken data, string id)
			{
				string APIPath = "Users/:id/accessTokens";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all accessTokens of this model.
			 */
			public static async Task deleteForUser(string id)
			{
				string APIPath = "Users/:id/accessTokens";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts accessTokens of User.
			 */
			public static async Task<double> countForUser(string id, string where = default(string))
			{
				string APIPath = "Users/:id/accessTokens/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}
		}
		public class UserCredentials : CRUDInterface<UserCredential>
		{

			/*
			 * Fetches belongsTo relation user.
			 */
			public static async Task<User> getUser(string id, bool refresh = default(bool))
			{
				string APIPath = "UserCredentials/:id/user";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<UserCredential> patchOrCreate(UserCredential data)
			{
				string APIPath = "UserCredentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<UserCredential> replaceOrCreate(UserCredential data)
			{
				string APIPath = "UserCredentials/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<UserCredential> upsertWithWhere(UserCredential data, string where = default(string))
			{
				string APIPath = "UserCredentials/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<UserCredential> replaceById(UserCredential data, string id)
			{
				string APIPath = "UserCredentials/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<UserCredential> patchAttributes(UserCredential data, string id)
			{
				string APIPath = "UserCredentials/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for credentials.
			 */
			public static async Task<UserCredential> findByIdForUser(string id, string fk)
			{
				string APIPath = "Users/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for credentials.
			 */
			public static async Task destroyByIdForUser(string id, string fk)
			{
				string APIPath = "Users/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for credentials.
			 */
			public static async Task<UserCredential> updateByIdForUser(UserCredential data, string id, string fk)
			{
				string APIPath = "Users/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries credentials of User.
			 */
			public static async Task<IList<UserCredential>> getForUser(string id, string filter = default(string))
			{
				string APIPath = "Users/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<UserCredential[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in credentials of this model.
			 */
			public static async Task<UserCredential> createForUser(UserCredential data, string id)
			{
				string APIPath = "Users/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all credentials of this model.
			 */
			public static async Task deleteForUser(string id)
			{
				string APIPath = "Users/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts credentials of User.
			 */
			public static async Task<double> countForUser(string id, string where = default(string))
			{
				string APIPath = "Users/:id/credentials/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}
		}
		public class UserIdentities : CRUDInterface<UserIdentity>
		{

			/*
			 * Fetches belongsTo relation user.
			 */
			public static async Task<User> getUser(string id, bool refresh = default(bool))
			{
				string APIPath = "UserIdentities/:id/user";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<UserIdentity> patchOrCreate(UserIdentity data)
			{
				string APIPath = "UserIdentities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<UserIdentity> replaceOrCreate(UserIdentity data)
			{
				string APIPath = "UserIdentities/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<UserIdentity> upsertWithWhere(UserIdentity data, string where = default(string))
			{
				string APIPath = "UserIdentities/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<UserIdentity> replaceById(UserIdentity data, string id)
			{
				string APIPath = "UserIdentities/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<UserIdentity> patchAttributes(UserIdentity data, string id)
			{
				string APIPath = "UserIdentities/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for identities.
			 */
			public static async Task<UserIdentity> findByIdForUser(string id, string fk)
			{
				string APIPath = "Users/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for identities.
			 */
			public static async Task destroyByIdForUser(string id, string fk)
			{
				string APIPath = "Users/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for identities.
			 */
			public static async Task<UserIdentity> updateByIdForUser(UserIdentity data, string id, string fk)
			{
				string APIPath = "Users/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries identities of User.
			 */
			public static async Task<IList<UserIdentity>> getForUser(string id, string filter = default(string))
			{
				string APIPath = "Users/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<UserIdentity[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in identities of this model.
			 */
			public static async Task<UserIdentity> createForUser(UserIdentity data, string id)
			{
				string APIPath = "Users/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all identities of this model.
			 */
			public static async Task deleteForUser(string id)
			{
				string APIPath = "Users/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts identities of User.
			 */
			public static async Task<double> countForUser(string id, string where = default(string))
			{
				string APIPath = "Users/:id/identities/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}
		}
		public class ACLs : CRUDInterface<ACL>
		{

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<ACL> patchOrCreate(ACL data)
			{
				string APIPath = "ACLs";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<ACL>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<ACL> replaceOrCreate(ACL data)
			{
				string APIPath = "ACLs/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<ACL>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<ACL> upsertWithWhere(ACL data, string where = default(string))
			{
				string APIPath = "ACLs/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<ACL>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<ACL> replaceById(ACL data, string id)
			{
				string APIPath = "ACLs/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<ACL>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<ACL> patchAttributes(ACL data, string id)
			{
				string APIPath = "ACLs/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<ACL>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class RoleMappings : CRUDInterface<RoleMapping>
		{

			/*
			 * Fetches belongsTo relation role.
			 */
			public static async Task<Role> getRole(string id, bool refresh = default(bool))
			{
				string APIPath = "RoleMappings/:id/role";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<RoleMapping> patchOrCreate(RoleMapping data)
			{
				string APIPath = "RoleMappings";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<RoleMapping> replaceOrCreate(RoleMapping data)
			{
				string APIPath = "RoleMappings/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<RoleMapping> upsertWithWhere(RoleMapping data, string where = default(string))
			{
				string APIPath = "RoleMappings/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<RoleMapping> replaceById(RoleMapping data, string id)
			{
				string APIPath = "RoleMappings/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<RoleMapping> patchAttributes(RoleMapping data, string id)
			{
				string APIPath = "RoleMappings/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for principals.
			 */
			public static async Task<RoleMapping> findByIdForRole(string id, string fk)
			{
				string APIPath = "Roles/:id/principals/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for principals.
			 */
			public static async Task destroyByIdForRole(string id, string fk)
			{
				string APIPath = "Roles/:id/principals/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for principals.
			 */
			public static async Task<RoleMapping> updateByIdForRole(RoleMapping data, string id, string fk)
			{
				string APIPath = "Roles/:id/principals/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries principals of Role.
			 */
			public static async Task<IList<RoleMapping>> getForRole(string id, string filter = default(string))
			{
				string APIPath = "Roles/:id/principals";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<RoleMapping[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in principals of this model.
			 */
			public static async Task<RoleMapping> createForRole(RoleMapping data, string id)
			{
				string APIPath = "Roles/:id/principals";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all principals of this model.
			 */
			public static async Task deleteForRole(string id)
			{
				string APIPath = "Roles/:id/principals";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts principals of Role.
			 */
			public static async Task<double> countForRole(string id, string where = default(string))
			{
				string APIPath = "Roles/:id/principals/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}
		}
		public class Roles : CRUDInterface<Role>
		{

			/*
			 * Find a related item by id for principals.
			 */
			public static async Task<RoleMapping> findByIdPrincipals(string id, string fk)
			{
				string APIPath = "Roles/:id/principals/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for principals.
			 */
			public static async Task destroyByIdPrincipals(string id, string fk)
			{
				string APIPath = "Roles/:id/principals/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for principals.
			 */
			public static async Task<RoleMapping> updateByIdPrincipals(Role data, string id, string fk)
			{
				string APIPath = "Roles/:id/principals/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries principals of Role.
			 */
			public static async Task<IList<RoleMapping>> getPrincipals(string id, string filter = default(string))
			{
				string APIPath = "Roles/:id/principals";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<RoleMapping[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in principals of this model.
			 */
			public static async Task<RoleMapping> createPrincipals(Role data, string id)
			{
				string APIPath = "Roles/:id/principals";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all principals of this model.
			 */
			public static async Task deletePrincipals(string id)
			{
				string APIPath = "Roles/:id/principals";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts principals of Role.
			 */
			public static async Task<double> countPrincipals(string id, string where = default(string))
			{
				string APIPath = "Roles/:id/principals/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Role> patchOrCreate(Role data)
			{
				string APIPath = "Roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Role> replaceOrCreate(Role data)
			{
				string APIPath = "Roles/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<Role> upsertWithWhere(Role data, string where = default(string))
			{
				string APIPath = "Roles/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Role> replaceById(Role data, string id)
			{
				string APIPath = "Roles/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Role> patchAttributes(Role data, string id)
			{
				string APIPath = "Roles/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Fetches belongsTo relation role.
			 */
			public static async Task<Role> getForRoleMapping(string id, bool refresh = default(bool))
			{
				string APIPath = "RoleMappings/:id/role";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class Enumerados : CRUDInterface<Enumerado>
		{

			/*
			 * Find a related item by id for aplicativos.
			 */
			public static async Task<Aplicativo> findByIdAplicativos(string id, string fk)
			{
				string APIPath = "enumerados/:id/aplicativos/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for aplicativos.
			 */
			public static async Task destroyByIdAplicativos(string id, string fk)
			{
				string APIPath = "enumerados/:id/aplicativos/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for aplicativos.
			 */
			public static async Task<Aplicativo> updateByIdAplicativos(Enumerado data, string id, string fk)
			{
				string APIPath = "enumerados/:id/aplicativos/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Add a related item by id for aplicativos.
			 */
			public static async Task<Aplicativo> linkAplicativos(string id, string fk)
			{
				string APIPath = "enumerados/:id/aplicativos/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Remove the aplicativos relation to an item by id.
			 */
			public static async Task unlinkAplicativos(string id, string fk)
			{
				string APIPath = "enumerados/:id/aplicativos/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Check the existence of aplicativos relation to an item by id.
			 */
			public static async Task<bool> existsAplicativos(string id, string fk)
			{
				string APIPath = "enumerados/:id/aplicativos/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<bool>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries enumeradoSono of enumerado.
			 */
			public static async Task<IList<Enumerado>> getEnumeradoSono(string filter = default(string))
			{
				string APIPath = "enumerados/enumeradoSono";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Enumerado[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in enumeradoSono of this model.
			 */
			public static async Task<Enumerado> createEnumeradoSono(Enumerado data)
			{
				string APIPath = "enumerados/enumeradoSono";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Enumerado>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all enumeradoSono of this model.
			 */
			public static async Task deleteEnumeradoSono()
			{
				string APIPath = "enumerados/enumeradoSono";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts enumeradoSono of enumerado.
			 */
			public static async Task<double> countEnumeradoSono(string where = default(string))
			{
				string APIPath = "enumerados/enumeradoSono/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Queries enumeradoCheckUp of enumerado.
			 */
			public static async Task<IList<Enumerado>> getEnumeradoCheckUp(string filter = default(string))
			{
				string APIPath = "enumerados/enumeradoCheckUp";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Enumerado[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in enumeradoCheckUp of this model.
			 */
			public static async Task<Enumerado> createEnumeradoCheckUp(Enumerado data)
			{
				string APIPath = "enumerados/enumeradoCheckUp";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Enumerado>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all enumeradoCheckUp of this model.
			 */
			public static async Task deleteEnumeradoCheckUp()
			{
				string APIPath = "enumerados/enumeradoCheckUp";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts enumeradoCheckUp of enumerado.
			 */
			public static async Task<double> countEnumeradoCheckUp(string where = default(string))
			{
				string APIPath = "enumerados/enumeradoCheckUp/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Queries enumeradoCentroDiagnostico of enumerado.
			 */
			public static async Task<IList<Enumerado>> getEnumeradoCentroDiagnostico(string filter = default(string))
			{
				string APIPath = "enumerados/enumeradoCentroDiagnostico";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Enumerado[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in enumeradoCentroDiagnostico of this model.
			 */
			public static async Task<Enumerado> createEnumeradoCentroDiagnostico(Enumerado data)
			{
				string APIPath = "enumerados/enumeradoCentroDiagnostico";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Enumerado>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all enumeradoCentroDiagnostico of this model.
			 */
			public static async Task deleteEnumeradoCentroDiagnostico()
			{
				string APIPath = "enumerados/enumeradoCentroDiagnostico";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts enumeradoCentroDiagnostico of enumerado.
			 */
			public static async Task<double> countEnumeradoCentroDiagnostico(string where = default(string))
			{
				string APIPath = "enumerados/enumeradoCentroDiagnostico/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Queries enumeradoMedicinaOcupacional of enumerado.
			 */
			public static async Task<IList<Enumerado>> getEnumeradoMedicinaOcupacional(string filter = default(string))
			{
				string APIPath = "enumerados/enumeradoMedicinaOcupacional";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Enumerado[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in enumeradoMedicinaOcupacional of this model.
			 */
			public static async Task<Enumerado> createEnumeradoMedicinaOcupacional(Enumerado data)
			{
				string APIPath = "enumerados/enumeradoMedicinaOcupacional";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Enumerado>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all enumeradoMedicinaOcupacional of this model.
			 */
			public static async Task deleteEnumeradoMedicinaOcupacional()
			{
				string APIPath = "enumerados/enumeradoMedicinaOcupacional";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts enumeradoMedicinaOcupacional of enumerado.
			 */
			public static async Task<double> countEnumeradoMedicinaOcupacional(string where = default(string))
			{
				string APIPath = "enumerados/enumeradoMedicinaOcupacional/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Queries aplicativos of enumerado.
			 */
			public static async Task<IList<Aplicativo>> getAplicativos(string id, string filter = default(string))
			{
				string APIPath = "enumerados/:id/aplicativos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Aplicativo[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in aplicativos of this model.
			 */
			public static async Task<Aplicativo> createAplicativos(Enumerado data, string id)
			{
				string APIPath = "enumerados/:id/aplicativos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all aplicativos of this model.
			 */
			public static async Task deleteAplicativos(string id)
			{
				string APIPath = "enumerados/:id/aplicativos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts aplicativos of enumerado.
			 */
			public static async Task<double> countAplicativos(string id, string where = default(string))
			{
				string APIPath = "enumerados/:id/aplicativos/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Enumerado> patchOrCreate(Enumerado data)
			{
				string APIPath = "enumerados";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Enumerado>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Enumerado> replaceOrCreate(Enumerado data)
			{
				string APIPath = "enumerados/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Enumerado>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<Enumerado> upsertWithWhere(Enumerado data, string where = default(string))
			{
				string APIPath = "enumerados/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<Enumerado>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Enumerado> replaceById(Enumerado data, string id)
			{
				string APIPath = "enumerados/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Enumerado>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Enumerado> patchAttributes(Enumerado data, string id)
			{
				string APIPath = "enumerados/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Enumerado>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Atualizar Cadastro de Enumerado.
			 */
			public static async Task<JObject> atualizarEnumerado(Enumerado data)
			{
				string APIPath = "enumerados/atualizarEnumerado";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<JObject>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class Parametros : CRUDInterface<Parametro>
		{

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Parametro> patchOrCreate(Parametro data)
			{
				string APIPath = "parametros";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Parametro>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Parametro> replaceOrCreate(Parametro data)
			{
				string APIPath = "parametros/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Parametro>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<Parametro> upsertWithWhere(Parametro data, string where = default(string))
			{
				string APIPath = "parametros/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<Parametro>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Parametro> replaceById(Parametro data, string id)
			{
				string APIPath = "parametros/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Parametro>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Parametro> patchAttributes(Parametro data, string id)
			{
				string APIPath = "parametros/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Parametro>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * No description given.
			 */
			public static async Task<JObject> getNumeroLivre(string numeroLivre = default(string))
			{
				string APIPath = "parametros/getNumeroLivre";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("numeroLivre", numeroLivre != null ? numeroLivre.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<JObject>();
			}

			/*
			 * No description given.
			 */
			public static async Task<JObject> getParametros()
			{
				string APIPath = "parametros/getParametros";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<JObject>();
			}
		}
		public class Jobs : CRUDInterface<Job>
		{

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Job> patchOrCreate(Job data)
			{
				string APIPath = "jobs";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Job>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Job> replaceOrCreate(Job data)
			{
				string APIPath = "jobs/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Job>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<Job> upsertWithWhere(Job data, string where = default(string))
			{
				string APIPath = "jobs/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<Job>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Job> replaceById(Job data, string id)
			{
				string APIPath = "jobs/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Job>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Job> patchAttributes(Job data, string id)
			{
				string APIPath = "jobs/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Job>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * No description given.
			 */
			public static async Task<JObject> sendEmail()
			{
				string APIPath = "jobs/sendEmail";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<JObject>();
			}

			/*
			 * No description given.
			 */
			public static async Task<JObject> execJob(string parametro = default(string))
			{
				string APIPath = "jobs/execJob";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("parametro", parametro != null ? parametro.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<JObject>();
			}
		}
		public class Paiss : CRUDInterface<Pais>
		{

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Pais> patchOrCreate(Pais data)
			{
				string APIPath = "pais";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Pais>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Pais> replaceOrCreate(Pais data)
			{
				string APIPath = "pais/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Pais>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<Pais> upsertWithWhere(Pais data, string where = default(string))
			{
				string APIPath = "pais/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<Pais>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Pais> replaceById(Pais data, string id)
			{
				string APIPath = "pais/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Pais>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Pais> patchAttributes(Pais data, string id)
			{
				string APIPath = "pais/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Pais>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * No description given.
			 */
			public static async Task<JObject> getEnderecoCompletoPorCEP(string parametro = default(string))
			{
				string APIPath = "pais/getEnderecoCompletoPorCEP";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("parametro", parametro != null ? parametro.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<JObject>();
			}
		}
		public class TextoPadraos : CRUDInterface<TextoPadrao>
		{

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<TextoPadrao> patchOrCreate(TextoPadrao data)
			{
				string APIPath = "textoPadraos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<TextoPadrao>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<TextoPadrao> replaceOrCreate(TextoPadrao data)
			{
				string APIPath = "textoPadraos/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<TextoPadrao>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<TextoPadrao> upsertWithWhere(TextoPadrao data, string where = default(string))
			{
				string APIPath = "textoPadraos/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<TextoPadrao>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<TextoPadrao> replaceById(TextoPadrao data, string id)
			{
				string APIPath = "textoPadraos/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<TextoPadrao>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<TextoPadrao> patchAttributes(TextoPadrao data, string id)
			{
				string APIPath = "textoPadraos/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<TextoPadrao>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * No description given.
			 */
			public static async Task<JObject> getTextoParametro(string codigoParametro = default(string))
			{
				string APIPath = "textoPadraos/getTextoParametro";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("codigoParametro", codigoParametro != null ? codigoParametro.ToString() : null);
				var response = await Gateway.PerformRequest<JObject>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class Pessoas : CRUDInterface<Pessoa>
		{

			/*
			 * Find a related item by id for user.
			 */
			public static async Task<User> findByIdUser(string id, string fk)
			{
				string APIPath = "pessoas/:id/user/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for user.
			 */
			public static async Task destroyByIdUser(string id, string fk)
			{
				string APIPath = "pessoas/:id/user/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for user.
			 */
			public static async Task<User> updateByIdUser(Pessoa data, string id, string fk)
			{
				string APIPath = "pessoas/:id/user/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Add a related item by id for user.
			 */
			public static async Task<User> linkUser(string id, string fk)
			{
				string APIPath = "pessoas/:id/user/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Remove the user relation to an item by id.
			 */
			public static async Task unlinkUser(string id, string fk)
			{
				string APIPath = "pessoas/:id/user/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Check the existence of user relation to an item by id.
			 */
			public static async Task<bool> existsUser(string id, string fk)
			{
				string APIPath = "pessoas/:id/user/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<bool>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for perfil.
			 */
			public static async Task<PerfilAcesso> findByIdPerfil(string id, string fk)
			{
				string APIPath = "pessoas/:id/perfil/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<PerfilAcesso>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for perfil.
			 */
			public static async Task destroyByIdPerfil(string id, string fk)
			{
				string APIPath = "pessoas/:id/perfil/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for perfil.
			 */
			public static async Task<PerfilAcesso> updateByIdPerfil(Pessoa data, string id, string fk)
			{
				string APIPath = "pessoas/:id/perfil/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<PerfilAcesso>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Add a related item by id for perfil.
			 */
			public static async Task<PerfilAcesso> linkPerfil(string id, string fk)
			{
				string APIPath = "pessoas/:id/perfil/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<PerfilAcesso>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Remove the perfil relation to an item by id.
			 */
			public static async Task unlinkPerfil(string id, string fk)
			{
				string APIPath = "pessoas/:id/perfil/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Check the existence of perfil relation to an item by id.
			 */
			public static async Task<bool> existsPerfil(string id, string fk)
			{
				string APIPath = "pessoas/:id/perfil/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<bool>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries user of pessoa.
			 */
			public static async Task<IList<User>> getUser(string id, string filter = default(string))
			{
				string APIPath = "pessoas/:id/user";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<User[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in user of this model.
			 */
			public static async Task<User> createUser(Pessoa data, string id)
			{
				string APIPath = "pessoas/:id/user";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all user of this model.
			 */
			public static async Task deleteUser(string id)
			{
				string APIPath = "pessoas/:id/user";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts user of pessoa.
			 */
			public static async Task<double> countUser(string id, string where = default(string))
			{
				string APIPath = "pessoas/:id/user/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Queries perfil of pessoa.
			 */
			public static async Task<IList<PerfilAcesso>> getPerfil(string id, string filter = default(string))
			{
				string APIPath = "pessoas/:id/perfil";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<PerfilAcesso[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in perfil of this model.
			 */
			public static async Task<PerfilAcesso> createPerfil(Pessoa data, string id)
			{
				string APIPath = "pessoas/:id/perfil";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<PerfilAcesso>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all perfil of this model.
			 */
			public static async Task deletePerfil(string id)
			{
				string APIPath = "pessoas/:id/perfil";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts perfil of pessoa.
			 */
			public static async Task<double> countPerfil(string id, string where = default(string))
			{
				string APIPath = "pessoas/:id/perfil/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Pessoa> patchOrCreate(Pessoa data)
			{
				string APIPath = "pessoas";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Pessoa> replaceOrCreate(Pessoa data)
			{
				string APIPath = "pessoas/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<Pessoa> upsertWithWhere(Pessoa data, string where = default(string))
			{
				string APIPath = "pessoas/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Pessoa> replaceById(Pessoa data, string id)
			{
				string APIPath = "pessoas/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Pessoa> patchAttributes(Pessoa data, string id)
			{
				string APIPath = "pessoas/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * No description given.
			 */
			public static async Task<JObject> criarLogin(CadastroPessoa data)
			{
				string APIPath = "pessoas/criarLogin";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<JObject>();
			}

			/*
			 * No description given.
			 */
			public static async Task<JObject> validarEmail(string user = default(string))
			{
				string APIPath = "pessoas/validarEmail";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("user", user != null ? user.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<JObject>();
			}

			/*
			 * No description given.
			 */
			public static async Task<JObject> getIdentidade(string userId = default(string))
			{
				string APIPath = "pessoas/getIdentidade";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("userId", userId != null ? userId.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<JObject>();
			}

			/*
			 * No description given.
			 */
			public static async Task<JObject> aplicarPerfilAcesso(string userId = default(string), string perfilAcesso = default(string))
			{
				string APIPath = "pessoas/setPerfilAcesso";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("userId", userId != null ? userId.ToString() : null);
				queryStrings.Add("perfilAcesso", perfilAcesso != null ? perfilAcesso.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<JObject>();
			}

			/*
			 * Fetches belongsTo relation cliente.
			 */
			public static async Task<Pessoa> getForagendamento(string id, bool refresh = default(bool))
			{
				string APIPath = "agendamentos/:id/cliente";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Fetches belongsTo relation atendente.
			 */
			public static async Task<Pessoa> getForagendamento1(string id, bool refresh = default(bool))
			{
				string APIPath = "agendamentos/:id/atendente";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Fetches belongsTo relation pessoa.
			 */
			public static async Task<Pessoa> getForinstalacao(string id, bool refresh = default(bool))
			{
				string APIPath = "instalacaos/:id/pessoa";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for pessoa.
			 */
			public static async Task<Pessoa> findByIdFornotificacao(string id, string fk)
			{
				string APIPath = "notificacaos/:id/pessoa/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for pessoa.
			 */
			public static async Task destroyByIdFornotificacao(string id, string fk)
			{
				string APIPath = "notificacaos/:id/pessoa/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for pessoa.
			 */
			public static async Task<Pessoa> updateByIdFornotificacao(Pessoa data, string id, string fk)
			{
				string APIPath = "notificacaos/:id/pessoa/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Add a related item by id for pessoa.
			 */
			public static async Task<Pessoa> linkFornotificacao(string id, string fk)
			{
				string APIPath = "notificacaos/:id/pessoa/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Remove the pessoa relation to an item by id.
			 */
			public static async Task unlinkFornotificacao(string id, string fk)
			{
				string APIPath = "notificacaos/:id/pessoa/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Check the existence of pessoa relation to an item by id.
			 */
			public static async Task<bool> existsFornotificacao(string id, string fk)
			{
				string APIPath = "notificacaos/:id/pessoa/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<bool>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries pessoa of notificacao.
			 */
			public static async Task<IList<Pessoa>> getFornotificacao(string id, string filter = default(string))
			{
				string APIPath = "notificacaos/:id/pessoa";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Pessoa[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in pessoa of this model.
			 */
			public static async Task<Pessoa> createFornotificacao(Pessoa data, string id)
			{
				string APIPath = "notificacaos/:id/pessoa";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all pessoa of this model.
			 */
			public static async Task deleteFornotificacao(string id)
			{
				string APIPath = "notificacaos/:id/pessoa";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts pessoa of notificacao.
			 */
			public static async Task<double> countFornotificacao(string id, string where = default(string))
			{
				string APIPath = "notificacaos/:id/pessoa/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Fetches belongsTo relation pessoa.
			 */
			public static async Task<Pessoa> getFordispositivo(string id, bool refresh = default(bool))
			{
				string APIPath = "dispositivos/:id/pessoa";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Fetches belongsTo relation pessoa.
			 */
			public static async Task<Pessoa> getForpessoaRegistro(string id, bool refresh = default(bool))
			{
				string APIPath = "pessoaRegistros/:id/pessoa";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Fetches belongsTo relation usuario.
			 */
			public static async Task<Pessoa> getForpessoaRegistro1(string id, bool refresh = default(bool))
			{
				string APIPath = "pessoaRegistros/:id/usuario";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Fetches belongsTo relation pessoa.
			 */
			public static async Task<Pessoa> getForpessoaAlergia(string id, bool refresh = default(bool))
			{
				string APIPath = "pessoaAlergia/:id/pessoa";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Fetches belongsTo relation pessoa.
			 */
			public static async Task<Pessoa> getForpessoaHistoricoFamiliar(string id, bool refresh = default(bool))
			{
				string APIPath = "pessoaHistoricoFamiliars/:id/pessoa";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Fetches belongsTo relation pessoa.
			 */
			public static async Task<Pessoa> getForpessoaProtocolo(string id, bool refresh = default(bool))
			{
				string APIPath = "pessoaProtocolos/:id/pessoa";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Fetches belongsTo relation pessoa.
			 */
			public static async Task<Pessoa> getForprotocolo(string id, bool refresh = default(bool))
			{
				string APIPath = "protocolos/:id/pessoa";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class PerfilAcessos : CRUDInterface<PerfilAcesso>
		{

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<PerfilAcesso> patchOrCreate(PerfilAcesso data)
			{
				string APIPath = "perfilAcessos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<PerfilAcesso>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<PerfilAcesso> replaceOrCreate(PerfilAcesso data)
			{
				string APIPath = "perfilAcessos/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<PerfilAcesso>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<PerfilAcesso> upsertWithWhere(PerfilAcesso data, string where = default(string))
			{
				string APIPath = "perfilAcessos/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<PerfilAcesso>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<PerfilAcesso> replaceById(PerfilAcesso data, string id)
			{
				string APIPath = "perfilAcessos/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<PerfilAcesso>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<PerfilAcesso> patchAttributes(PerfilAcesso data, string id)
			{
				string APIPath = "perfilAcessos/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<PerfilAcesso>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for perfil.
			 */
			public static async Task<PerfilAcesso> findByIdForpessoa(string id, string fk)
			{
				string APIPath = "pessoas/:id/perfil/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<PerfilAcesso>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for perfil.
			 */
			public static async Task destroyByIdForpessoa(string id, string fk)
			{
				string APIPath = "pessoas/:id/perfil/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for perfil.
			 */
			public static async Task<PerfilAcesso> updateByIdForpessoa(PerfilAcesso data, string id, string fk)
			{
				string APIPath = "pessoas/:id/perfil/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<PerfilAcesso>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Add a related item by id for perfil.
			 */
			public static async Task<PerfilAcesso> linkForpessoa(string id, string fk)
			{
				string APIPath = "pessoas/:id/perfil/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<PerfilAcesso>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Remove the perfil relation to an item by id.
			 */
			public static async Task unlinkForpessoa(string id, string fk)
			{
				string APIPath = "pessoas/:id/perfil/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Check the existence of perfil relation to an item by id.
			 */
			public static async Task<bool> existsForpessoa(string id, string fk)
			{
				string APIPath = "pessoas/:id/perfil/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<bool>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries perfil of pessoa.
			 */
			public static async Task<IList<PerfilAcesso>> getForpessoa(string id, string filter = default(string))
			{
				string APIPath = "pessoas/:id/perfil";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<PerfilAcesso[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in perfil of this model.
			 */
			public static async Task<PerfilAcesso> createForpessoa(PerfilAcesso data, string id)
			{
				string APIPath = "pessoas/:id/perfil";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<PerfilAcesso>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all perfil of this model.
			 */
			public static async Task deleteForpessoa(string id)
			{
				string APIPath = "pessoas/:id/perfil";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts perfil of pessoa.
			 */
			public static async Task<double> countForpessoa(string id, string where = default(string))
			{
				string APIPath = "pessoas/:id/perfil/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}
		}
		public class Rotas : CRUDInterface<Rota>
		{

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Rota> patchOrCreate(Rota data)
			{
				string APIPath = "rota";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Rota>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Rota> replaceOrCreate(Rota data)
			{
				string APIPath = "rota/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Rota>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<Rota> upsertWithWhere(Rota data, string where = default(string))
			{
				string APIPath = "rota/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<Rota>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Rota> replaceById(Rota data, string id)
			{
				string APIPath = "rota/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Rota>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Rota> patchAttributes(Rota data, string id)
			{
				string APIPath = "rota/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Rota>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * No description given.
			 */
			public static async Task<bool> validarPermissao(string id = default(string))
			{
				string APIPath = "rota/validarPermissao";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("id", id != null ? id.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<bool>();
			}

			/*
			 * No description given.
			 */
			public static async Task<IList<string>> getConfigRota()
			{
				string APIPath = "rota/getConfigRota";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				var response = await Gateway.PerformRequest<string[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class Periodicidades : CRUDInterface<Periodicidade>
		{

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Periodicidade> patchOrCreate(Periodicidade data)
			{
				string APIPath = "periodicidades";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Periodicidade>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Periodicidade> replaceOrCreate(Periodicidade data)
			{
				string APIPath = "periodicidades/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Periodicidade>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<Periodicidade> upsertWithWhere(Periodicidade data, string where = default(string))
			{
				string APIPath = "periodicidades/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<Periodicidade>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Periodicidade> replaceById(Periodicidade data, string id)
			{
				string APIPath = "periodicidades/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Periodicidade>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Periodicidade> patchAttributes(Periodicidade data, string id)
			{
				string APIPath = "periodicidades/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Periodicidade>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for periodicidade.
			 */
			public static async Task<Periodicidade> findByIdForproduto(string id, string fk)
			{
				string APIPath = "produtos/:id/periodicidade/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Periodicidade>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for periodicidade.
			 */
			public static async Task destroyByIdForproduto(string id, string fk)
			{
				string APIPath = "produtos/:id/periodicidade/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for periodicidade.
			 */
			public static async Task<Periodicidade> updateByIdForproduto(Periodicidade data, string id, string fk)
			{
				string APIPath = "produtos/:id/periodicidade/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Periodicidade>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries periodicidade of produto.
			 */
			public static async Task<IList<Periodicidade>> getForproduto(string id, string filter = default(string))
			{
				string APIPath = "produtos/:id/periodicidade";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Periodicidade[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in periodicidade of this model.
			 */
			public static async Task<Periodicidade> createForproduto(Periodicidade data, string id)
			{
				string APIPath = "produtos/:id/periodicidade";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Periodicidade>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all periodicidade of this model.
			 */
			public static async Task deleteForproduto(string id)
			{
				string APIPath = "produtos/:id/periodicidade";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts periodicidade of produto.
			 */
			public static async Task<double> countForproduto(string id, string where = default(string))
			{
				string APIPath = "produtos/:id/periodicidade/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}
		}
		public class TipoServicos : CRUDInterface<TipoServico>
		{

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<TipoServico> patchOrCreate(TipoServico data)
			{
				string APIPath = "tipoServicos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<TipoServico>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<TipoServico> replaceOrCreate(TipoServico data)
			{
				string APIPath = "tipoServicos/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<TipoServico>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<TipoServico> upsertWithWhere(TipoServico data, string where = default(string))
			{
				string APIPath = "tipoServicos/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<TipoServico>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<TipoServico> replaceById(TipoServico data, string id)
			{
				string APIPath = "tipoServicos/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<TipoServico>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<TipoServico> patchAttributes(TipoServico data, string id)
			{
				string APIPath = "tipoServicos/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<TipoServico>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for tipoServico.
			 */
			public static async Task<TipoServico> findByIdForproduto(string id, string fk)
			{
				string APIPath = "produtos/:id/tipoServico/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<TipoServico>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for tipoServico.
			 */
			public static async Task destroyByIdForproduto(string id, string fk)
			{
				string APIPath = "produtos/:id/tipoServico/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for tipoServico.
			 */
			public static async Task<TipoServico> updateByIdForproduto(TipoServico data, string id, string fk)
			{
				string APIPath = "produtos/:id/tipoServico/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<TipoServico>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries tipoServico of produto.
			 */
			public static async Task<IList<TipoServico>> getForproduto(string id, string filter = default(string))
			{
				string APIPath = "produtos/:id/tipoServico";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<TipoServico[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in tipoServico of this model.
			 */
			public static async Task<TipoServico> createForproduto(TipoServico data, string id)
			{
				string APIPath = "produtos/:id/tipoServico";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<TipoServico>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all tipoServico of this model.
			 */
			public static async Task deleteForproduto(string id)
			{
				string APIPath = "produtos/:id/tipoServico";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts tipoServico of produto.
			 */
			public static async Task<double> countForproduto(string id, string where = default(string))
			{
				string APIPath = "produtos/:id/tipoServico/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}
		}
		public class Produtos : CRUDInterface<Produto>
		{

			/*
			 * Find a related item by id for periodicidade.
			 */
			public static async Task<Periodicidade> findByIdPeriodicidade(string id, string fk)
			{
				string APIPath = "produtos/:id/periodicidade/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Periodicidade>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for periodicidade.
			 */
			public static async Task destroyByIdPeriodicidade(string id, string fk)
			{
				string APIPath = "produtos/:id/periodicidade/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for periodicidade.
			 */
			public static async Task<Periodicidade> updateByIdPeriodicidade(Produto data, string id, string fk)
			{
				string APIPath = "produtos/:id/periodicidade/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Periodicidade>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for tipoServico.
			 */
			public static async Task<TipoServico> findByIdTipoServico(string id, string fk)
			{
				string APIPath = "produtos/:id/tipoServico/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<TipoServico>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for tipoServico.
			 */
			public static async Task destroyByIdTipoServico(string id, string fk)
			{
				string APIPath = "produtos/:id/tipoServico/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for tipoServico.
			 */
			public static async Task<TipoServico> updateByIdTipoServico(Produto data, string id, string fk)
			{
				string APIPath = "produtos/:id/tipoServico/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<TipoServico>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries periodicidade of produto.
			 */
			public static async Task<IList<Periodicidade>> getPeriodicidade(string id, string filter = default(string))
			{
				string APIPath = "produtos/:id/periodicidade";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Periodicidade[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in periodicidade of this model.
			 */
			public static async Task<Periodicidade> createPeriodicidade(Produto data, string id)
			{
				string APIPath = "produtos/:id/periodicidade";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Periodicidade>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all periodicidade of this model.
			 */
			public static async Task deletePeriodicidade(string id)
			{
				string APIPath = "produtos/:id/periodicidade";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts periodicidade of produto.
			 */
			public static async Task<double> countPeriodicidade(string id, string where = default(string))
			{
				string APIPath = "produtos/:id/periodicidade/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Queries tipoServico of produto.
			 */
			public static async Task<IList<TipoServico>> getTipoServico(string id, string filter = default(string))
			{
				string APIPath = "produtos/:id/tipoServico";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<TipoServico[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in tipoServico of this model.
			 */
			public static async Task<TipoServico> createTipoServico(Produto data, string id)
			{
				string APIPath = "produtos/:id/tipoServico";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<TipoServico>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all tipoServico of this model.
			 */
			public static async Task deleteTipoServico(string id)
			{
				string APIPath = "produtos/:id/tipoServico";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts tipoServico of produto.
			 */
			public static async Task<double> countTipoServico(string id, string where = default(string))
			{
				string APIPath = "produtos/:id/tipoServico/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Produto> patchOrCreate(Produto data)
			{
				string APIPath = "produtos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Produto>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Produto> replaceOrCreate(Produto data)
			{
				string APIPath = "produtos/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Produto>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<Produto> upsertWithWhere(Produto data, string where = default(string))
			{
				string APIPath = "produtos/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<Produto>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Produto> replaceById(Produto data, string id)
			{
				string APIPath = "produtos/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Produto>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Produto> patchAttributes(Produto data, string id)
			{
				string APIPath = "produtos/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Produto>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class Dicas : CRUDInterface<Dica>
		{

			/*
			 * Find a related item by id for aplicativos.
			 */
			public static async Task<Aplicativo> findByIdAplicativos(string id, string fk)
			{
				string APIPath = "dicas/:id/aplicativos/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for aplicativos.
			 */
			public static async Task destroyByIdAplicativos(string id, string fk)
			{
				string APIPath = "dicas/:id/aplicativos/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for aplicativos.
			 */
			public static async Task<Aplicativo> updateByIdAplicativos(Dica data, string id, string fk)
			{
				string APIPath = "dicas/:id/aplicativos/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Add a related item by id for aplicativos.
			 */
			public static async Task<Aplicativo> linkAplicativos(string id, string fk)
			{
				string APIPath = "dicas/:id/aplicativos/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Remove the aplicativos relation to an item by id.
			 */
			public static async Task unlinkAplicativos(string id, string fk)
			{
				string APIPath = "dicas/:id/aplicativos/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Check the existence of aplicativos relation to an item by id.
			 */
			public static async Task<bool> existsAplicativos(string id, string fk)
			{
				string APIPath = "dicas/:id/aplicativos/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<bool>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries dicaSono of dica.
			 */
			public static async Task<IList<Dica>> getDicaSono(string filter = default(string))
			{
				string APIPath = "dicas/dicaSono";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Dica[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in dicaSono of this model.
			 */
			public static async Task<Dica> createDicaSono(Dica data)
			{
				string APIPath = "dicas/dicaSono";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Dica>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all dicaSono of this model.
			 */
			public static async Task deleteDicaSono()
			{
				string APIPath = "dicas/dicaSono";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts dicaSono of dica.
			 */
			public static async Task<double> countDicaSono(string where = default(string))
			{
				string APIPath = "dicas/dicaSono/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Queries dicaCheckUp of dica.
			 */
			public static async Task<IList<Dica>> getDicaCheckUp(string filter = default(string))
			{
				string APIPath = "dicas/dicaCheckUp";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Dica[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in dicaCheckUp of this model.
			 */
			public static async Task<Dica> createDicaCheckUp(Dica data)
			{
				string APIPath = "dicas/dicaCheckUp";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Dica>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all dicaCheckUp of this model.
			 */
			public static async Task deleteDicaCheckUp()
			{
				string APIPath = "dicas/dicaCheckUp";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts dicaCheckUp of dica.
			 */
			public static async Task<double> countDicaCheckUp(string where = default(string))
			{
				string APIPath = "dicas/dicaCheckUp/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Queries dicaCentroDiagnostico of dica.
			 */
			public static async Task<IList<Dica>> getDicaCentroDiagnostico(string filter = default(string))
			{
				string APIPath = "dicas/dicaCentroDiagnostico";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Dica[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in dicaCentroDiagnostico of this model.
			 */
			public static async Task<Dica> createDicaCentroDiagnostico(Dica data)
			{
				string APIPath = "dicas/dicaCentroDiagnostico";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Dica>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all dicaCentroDiagnostico of this model.
			 */
			public static async Task deleteDicaCentroDiagnostico()
			{
				string APIPath = "dicas/dicaCentroDiagnostico";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts dicaCentroDiagnostico of dica.
			 */
			public static async Task<double> countDicaCentroDiagnostico(string where = default(string))
			{
				string APIPath = "dicas/dicaCentroDiagnostico/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Queries dicaMedicinaOcupacional of dica.
			 */
			public static async Task<IList<Dica>> getDicaMedicinaOcupacional(string filter = default(string))
			{
				string APIPath = "dicas/dicaMedicinaOcupacional";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Dica[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in dicaMedicinaOcupacional of this model.
			 */
			public static async Task<Dica> createDicaMedicinaOcupacional(Dica data)
			{
				string APIPath = "dicas/dicaMedicinaOcupacional";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Dica>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all dicaMedicinaOcupacional of this model.
			 */
			public static async Task deleteDicaMedicinaOcupacional()
			{
				string APIPath = "dicas/dicaMedicinaOcupacional";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts dicaMedicinaOcupacional of dica.
			 */
			public static async Task<double> countDicaMedicinaOcupacional(string where = default(string))
			{
				string APIPath = "dicas/dicaMedicinaOcupacional/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Queries aplicativos of dica.
			 */
			public static async Task<IList<Aplicativo>> getAplicativos(string id, string filter = default(string))
			{
				string APIPath = "dicas/:id/aplicativos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Aplicativo[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in aplicativos of this model.
			 */
			public static async Task<Aplicativo> createAplicativos(Dica data, string id)
			{
				string APIPath = "dicas/:id/aplicativos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all aplicativos of this model.
			 */
			public static async Task deleteAplicativos(string id)
			{
				string APIPath = "dicas/:id/aplicativos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts aplicativos of dica.
			 */
			public static async Task<double> countAplicativos(string id, string where = default(string))
			{
				string APIPath = "dicas/:id/aplicativos/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Dica> patchOrCreate(Dica data)
			{
				string APIPath = "dicas";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Dica>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Dica> replaceOrCreate(Dica data)
			{
				string APIPath = "dicas/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Dica>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<Dica> upsertWithWhere(Dica data, string where = default(string))
			{
				string APIPath = "dicas/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<Dica>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Dica> replaceById(Dica data, string id)
			{
				string APIPath = "dicas/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Dica>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Dica> patchAttributes(Dica data, string id)
			{
				string APIPath = "dicas/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Dica>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class Aplicativos : CRUDInterface<Aplicativo>
		{

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Aplicativo> patchOrCreate(Aplicativo data)
			{
				string APIPath = "aplicativos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Aplicativo> replaceOrCreate(Aplicativo data)
			{
				string APIPath = "aplicativos/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<Aplicativo> upsertWithWhere(Aplicativo data, string where = default(string))
			{
				string APIPath = "aplicativos/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Aplicativo> replaceById(Aplicativo data, string id)
			{
				string APIPath = "aplicativos/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Aplicativo> patchAttributes(Aplicativo data, string id)
			{
				string APIPath = "aplicativos/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for aplicativos.
			 */
			public static async Task<Aplicativo> findByIdForenumerado(string id, string fk)
			{
				string APIPath = "enumerados/:id/aplicativos/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for aplicativos.
			 */
			public static async Task destroyByIdForenumerado(string id, string fk)
			{
				string APIPath = "enumerados/:id/aplicativos/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for aplicativos.
			 */
			public static async Task<Aplicativo> updateByIdForenumerado(Aplicativo data, string id, string fk)
			{
				string APIPath = "enumerados/:id/aplicativos/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Add a related item by id for aplicativos.
			 */
			public static async Task<Aplicativo> linkForenumerado(string id, string fk)
			{
				string APIPath = "enumerados/:id/aplicativos/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Remove the aplicativos relation to an item by id.
			 */
			public static async Task unlinkForenumerado(string id, string fk)
			{
				string APIPath = "enumerados/:id/aplicativos/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Check the existence of aplicativos relation to an item by id.
			 */
			public static async Task<bool> existsForenumerado(string id, string fk)
			{
				string APIPath = "enumerados/:id/aplicativos/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<bool>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries aplicativos of enumerado.
			 */
			public static async Task<IList<Aplicativo>> getForenumerado(string id, string filter = default(string))
			{
				string APIPath = "enumerados/:id/aplicativos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Aplicativo[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in aplicativos of this model.
			 */
			public static async Task<Aplicativo> createForenumerado(Aplicativo data, string id)
			{
				string APIPath = "enumerados/:id/aplicativos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all aplicativos of this model.
			 */
			public static async Task deleteForenumerado(string id)
			{
				string APIPath = "enumerados/:id/aplicativos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts aplicativos of enumerado.
			 */
			public static async Task<double> countForenumerado(string id, string where = default(string))
			{
				string APIPath = "enumerados/:id/aplicativos/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Find a related item by id for aplicativos.
			 */
			public static async Task<Aplicativo> findByIdFordica(string id, string fk)
			{
				string APIPath = "dicas/:id/aplicativos/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for aplicativos.
			 */
			public static async Task destroyByIdFordica(string id, string fk)
			{
				string APIPath = "dicas/:id/aplicativos/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for aplicativos.
			 */
			public static async Task<Aplicativo> updateByIdFordica(Aplicativo data, string id, string fk)
			{
				string APIPath = "dicas/:id/aplicativos/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Add a related item by id for aplicativos.
			 */
			public static async Task<Aplicativo> linkFordica(string id, string fk)
			{
				string APIPath = "dicas/:id/aplicativos/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Remove the aplicativos relation to an item by id.
			 */
			public static async Task unlinkFordica(string id, string fk)
			{
				string APIPath = "dicas/:id/aplicativos/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Check the existence of aplicativos relation to an item by id.
			 */
			public static async Task<bool> existsFordica(string id, string fk)
			{
				string APIPath = "dicas/:id/aplicativos/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<bool>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries aplicativos of dica.
			 */
			public static async Task<IList<Aplicativo>> getFordica(string id, string filter = default(string))
			{
				string APIPath = "dicas/:id/aplicativos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Aplicativo[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in aplicativos of this model.
			 */
			public static async Task<Aplicativo> createFordica(Aplicativo data, string id)
			{
				string APIPath = "dicas/:id/aplicativos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all aplicativos of this model.
			 */
			public static async Task deleteFordica(string id)
			{
				string APIPath = "dicas/:id/aplicativos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts aplicativos of dica.
			 */
			public static async Task<double> countFordica(string id, string where = default(string))
			{
				string APIPath = "dicas/:id/aplicativos/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Find a related item by id for aplicativos.
			 */
			public static async Task<Aplicativo> findByIdForexamePreparo(string id, string fk)
			{
				string APIPath = "examePreparos/:id/aplicativos/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for aplicativos.
			 */
			public static async Task destroyByIdForexamePreparo(string id, string fk)
			{
				string APIPath = "examePreparos/:id/aplicativos/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for aplicativos.
			 */
			public static async Task<Aplicativo> updateByIdForexamePreparo(Aplicativo data, string id, string fk)
			{
				string APIPath = "examePreparos/:id/aplicativos/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Add a related item by id for aplicativos.
			 */
			public static async Task<Aplicativo> linkForexamePreparo(string id, string fk)
			{
				string APIPath = "examePreparos/:id/aplicativos/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Remove the aplicativos relation to an item by id.
			 */
			public static async Task unlinkForexamePreparo(string id, string fk)
			{
				string APIPath = "examePreparos/:id/aplicativos/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Check the existence of aplicativos relation to an item by id.
			 */
			public static async Task<bool> existsForexamePreparo(string id, string fk)
			{
				string APIPath = "examePreparos/:id/aplicativos/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<bool>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries aplicativos of examePreparo.
			 */
			public static async Task<IList<Aplicativo>> getForexamePreparo(string id, string filter = default(string))
			{
				string APIPath = "examePreparos/:id/aplicativos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Aplicativo[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in aplicativos of this model.
			 */
			public static async Task<Aplicativo> createForexamePreparo(Aplicativo data, string id)
			{
				string APIPath = "examePreparos/:id/aplicativos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all aplicativos of this model.
			 */
			public static async Task deleteForexamePreparo(string id)
			{
				string APIPath = "examePreparos/:id/aplicativos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts aplicativos of examePreparo.
			 */
			public static async Task<double> countForexamePreparo(string id, string where = default(string))
			{
				string APIPath = "examePreparos/:id/aplicativos/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Fetches belongsTo relation aplicativo.
			 */
			public static async Task<Aplicativo> getForinstalacao(string id, bool refresh = default(bool))
			{
				string APIPath = "instalacaos/:id/aplicativo";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Fetches belongsTo relation aplicativo.
			 */
			public static async Task<Aplicativo> getFornotificacao(string id, bool refresh = default(bool))
			{
				string APIPath = "notificacaos/:id/aplicativo";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Fetches belongsTo relation aplicativo.
			 */
			public static async Task<Aplicativo> getFordispositivo(string id, bool refresh = default(bool))
			{
				string APIPath = "dispositivos/:id/aplicativo";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class ExamePreparos : CRUDInterface<ExamePreparo>
		{

			/*
			 * Find a related item by id for aplicativos.
			 */
			public static async Task<Aplicativo> findByIdAplicativos(string id, string fk)
			{
				string APIPath = "examePreparos/:id/aplicativos/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for aplicativos.
			 */
			public static async Task destroyByIdAplicativos(string id, string fk)
			{
				string APIPath = "examePreparos/:id/aplicativos/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for aplicativos.
			 */
			public static async Task<Aplicativo> updateByIdAplicativos(ExamePreparo data, string id, string fk)
			{
				string APIPath = "examePreparos/:id/aplicativos/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Add a related item by id for aplicativos.
			 */
			public static async Task<Aplicativo> linkAplicativos(string id, string fk)
			{
				string APIPath = "examePreparos/:id/aplicativos/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Remove the aplicativos relation to an item by id.
			 */
			public static async Task unlinkAplicativos(string id, string fk)
			{
				string APIPath = "examePreparos/:id/aplicativos/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Check the existence of aplicativos relation to an item by id.
			 */
			public static async Task<bool> existsAplicativos(string id, string fk)
			{
				string APIPath = "examePreparos/:id/aplicativos/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<bool>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for detalhePreparo.
			 */
			public static async Task<ExameDetalhe> findByIdDetalhePreparo(string id, string fk)
			{
				string APIPath = "examePreparos/:id/detalhePreparo/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<ExameDetalhe>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for detalhePreparo.
			 */
			public static async Task destroyByIdDetalhePreparo(string id, string fk)
			{
				string APIPath = "examePreparos/:id/detalhePreparo/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for detalhePreparo.
			 */
			public static async Task<ExameDetalhe> updateByIdDetalhePreparo(ExamePreparo data, string id, string fk)
			{
				string APIPath = "examePreparos/:id/detalhePreparo/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<ExameDetalhe>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries aplicativos of examePreparo.
			 */
			public static async Task<IList<Aplicativo>> getAplicativos(string id, string filter = default(string))
			{
				string APIPath = "examePreparos/:id/aplicativos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Aplicativo[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in aplicativos of this model.
			 */
			public static async Task<Aplicativo> createAplicativos(ExamePreparo data, string id)
			{
				string APIPath = "examePreparos/:id/aplicativos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all aplicativos of this model.
			 */
			public static async Task deleteAplicativos(string id)
			{
				string APIPath = "examePreparos/:id/aplicativos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts aplicativos of examePreparo.
			 */
			public static async Task<double> countAplicativos(string id, string where = default(string))
			{
				string APIPath = "examePreparos/:id/aplicativos/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Queries preparoSono of examePreparo.
			 */
			public static async Task<IList<ExamePreparo>> getPreparoSono(string filter = default(string))
			{
				string APIPath = "examePreparos/preparoSono";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<ExamePreparo[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in preparoSono of this model.
			 */
			public static async Task<ExamePreparo> createPreparoSono(ExamePreparo data)
			{
				string APIPath = "examePreparos/preparoSono";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<ExamePreparo>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all preparoSono of this model.
			 */
			public static async Task deletePreparoSono()
			{
				string APIPath = "examePreparos/preparoSono";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts preparoSono of examePreparo.
			 */
			public static async Task<double> countPreparoSono(string where = default(string))
			{
				string APIPath = "examePreparos/preparoSono/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Queries preparoCheckUp of examePreparo.
			 */
			public static async Task<IList<ExamePreparo>> getPreparoCheckUp(string filter = default(string))
			{
				string APIPath = "examePreparos/preparoCheckUp";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<ExamePreparo[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in preparoCheckUp of this model.
			 */
			public static async Task<ExamePreparo> createPreparoCheckUp(ExamePreparo data)
			{
				string APIPath = "examePreparos/preparoCheckUp";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<ExamePreparo>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all preparoCheckUp of this model.
			 */
			public static async Task deletePreparoCheckUp()
			{
				string APIPath = "examePreparos/preparoCheckUp";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts preparoCheckUp of examePreparo.
			 */
			public static async Task<double> countPreparoCheckUp(string where = default(string))
			{
				string APIPath = "examePreparos/preparoCheckUp/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Queries preparoCentroDiagnostico of examePreparo.
			 */
			public static async Task<IList<ExamePreparo>> getPreparoCentroDiagnostico(string filter = default(string))
			{
				string APIPath = "examePreparos/preparoCentroDiagnostico";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<ExamePreparo[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in preparoCentroDiagnostico of this model.
			 */
			public static async Task<ExamePreparo> createPreparoCentroDiagnostico(ExamePreparo data)
			{
				string APIPath = "examePreparos/preparoCentroDiagnostico";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<ExamePreparo>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all preparoCentroDiagnostico of this model.
			 */
			public static async Task deletePreparoCentroDiagnostico()
			{
				string APIPath = "examePreparos/preparoCentroDiagnostico";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts preparoCentroDiagnostico of examePreparo.
			 */
			public static async Task<double> countPreparoCentroDiagnostico(string where = default(string))
			{
				string APIPath = "examePreparos/preparoCentroDiagnostico/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Queries preparoMedicinaOcupacional of examePreparo.
			 */
			public static async Task<IList<ExamePreparo>> getPreparoMedicinaOcupacional(string filter = default(string))
			{
				string APIPath = "examePreparos/preparoMedicinaOcupacional";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<ExamePreparo[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in preparoMedicinaOcupacional of this model.
			 */
			public static async Task<ExamePreparo> createPreparoMedicinaOcupacional(ExamePreparo data)
			{
				string APIPath = "examePreparos/preparoMedicinaOcupacional";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<ExamePreparo>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all preparoMedicinaOcupacional of this model.
			 */
			public static async Task deletePreparoMedicinaOcupacional()
			{
				string APIPath = "examePreparos/preparoMedicinaOcupacional";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts preparoMedicinaOcupacional of examePreparo.
			 */
			public static async Task<double> countPreparoMedicinaOcupacional(string where = default(string))
			{
				string APIPath = "examePreparos/preparoMedicinaOcupacional/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Queries detalhePreparo of examePreparo.
			 */
			public static async Task<IList<ExameDetalhe>> getDetalhePreparo(string id, string filter = default(string))
			{
				string APIPath = "examePreparos/:id/detalhePreparo";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<ExameDetalhe[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in detalhePreparo of this model.
			 */
			public static async Task<ExameDetalhe> createDetalhePreparo(ExamePreparo data, string id)
			{
				string APIPath = "examePreparos/:id/detalhePreparo";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<ExameDetalhe>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all detalhePreparo of this model.
			 */
			public static async Task deleteDetalhePreparo(string id)
			{
				string APIPath = "examePreparos/:id/detalhePreparo";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts detalhePreparo of examePreparo.
			 */
			public static async Task<double> countDetalhePreparo(string id, string where = default(string))
			{
				string APIPath = "examePreparos/:id/detalhePreparo/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<ExamePreparo> patchOrCreate(ExamePreparo data)
			{
				string APIPath = "examePreparos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<ExamePreparo>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<ExamePreparo> replaceOrCreate(ExamePreparo data)
			{
				string APIPath = "examePreparos/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<ExamePreparo>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<ExamePreparo> upsertWithWhere(ExamePreparo data, string where = default(string))
			{
				string APIPath = "examePreparos/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<ExamePreparo>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<ExamePreparo> replaceById(ExamePreparo data, string id)
			{
				string APIPath = "examePreparos/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<ExamePreparo>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<ExamePreparo> patchAttributes(ExamePreparo data, string id)
			{
				string APIPath = "examePreparos/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<ExamePreparo>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class Agendamentos : CRUDInterface<Agendamento>
		{

			/*
			 * Fetches belongsTo relation cliente.
			 */
			public static async Task<Pessoa> getCliente(string id, bool refresh = default(bool))
			{
				string APIPath = "agendamentos/:id/cliente";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Fetches belongsTo relation atendente.
			 */
			public static async Task<Pessoa> getAtendente(string id, bool refresh = default(bool))
			{
				string APIPath = "agendamentos/:id/atendente";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Agendamento> patchOrCreate(Agendamento data)
			{
				string APIPath = "agendamentos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Agendamento>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Agendamento> replaceOrCreate(Agendamento data)
			{
				string APIPath = "agendamentos/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Agendamento>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<Agendamento> upsertWithWhere(Agendamento data, string where = default(string))
			{
				string APIPath = "agendamentos/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<Agendamento>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Agendamento> replaceById(Agendamento data, string id)
			{
				string APIPath = "agendamentos/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Agendamento>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Agendamento> patchAttributes(Agendamento data, string id)
			{
				string APIPath = "agendamentos/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Agendamento>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class Instalacaos : CRUDInterface<Instalacao>
		{

			/*
			 * Fetches belongsTo relation pessoa.
			 */
			public static async Task<Pessoa> getPessoa(string id, bool refresh = default(bool))
			{
				string APIPath = "instalacaos/:id/pessoa";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Fetches belongsTo relation aplicativo.
			 */
			public static async Task<Aplicativo> getAplicativo(string id, bool refresh = default(bool))
			{
				string APIPath = "instalacaos/:id/aplicativo";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Instalacao> patchOrCreate(Instalacao data)
			{
				string APIPath = "instalacaos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Instalacao>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Instalacao> replaceOrCreate(Instalacao data)
			{
				string APIPath = "instalacaos/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Instalacao>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<Instalacao> upsertWithWhere(Instalacao data, string where = default(string))
			{
				string APIPath = "instalacaos/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<Instalacao>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Instalacao> replaceById(Instalacao data, string id)
			{
				string APIPath = "instalacaos/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Instalacao>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Instalacao> patchAttributes(Instalacao data, string id)
			{
				string APIPath = "instalacaos/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Instalacao>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class Notificacaos : CRUDInterface<Notificacao>
		{

			/*
			 * Find a related item by id for pessoa.
			 */
			public static async Task<Pessoa> findByIdPessoa(string id, string fk)
			{
				string APIPath = "notificacaos/:id/pessoa/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for pessoa.
			 */
			public static async Task destroyByIdPessoa(string id, string fk)
			{
				string APIPath = "notificacaos/:id/pessoa/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for pessoa.
			 */
			public static async Task<Pessoa> updateByIdPessoa(Notificacao data, string id, string fk)
			{
				string APIPath = "notificacaos/:id/pessoa/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Add a related item by id for pessoa.
			 */
			public static async Task<Pessoa> linkPessoa(string id, string fk)
			{
				string APIPath = "notificacaos/:id/pessoa/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Remove the pessoa relation to an item by id.
			 */
			public static async Task unlinkPessoa(string id, string fk)
			{
				string APIPath = "notificacaos/:id/pessoa/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Check the existence of pessoa relation to an item by id.
			 */
			public static async Task<bool> existsPessoa(string id, string fk)
			{
				string APIPath = "notificacaos/:id/pessoa/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<bool>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Fetches belongsTo relation aplicativo.
			 */
			public static async Task<Aplicativo> getAplicativo(string id, bool refresh = default(bool))
			{
				string APIPath = "notificacaos/:id/aplicativo";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries pessoa of notificacao.
			 */
			public static async Task<IList<Pessoa>> getPessoa(string id, string filter = default(string))
			{
				string APIPath = "notificacaos/:id/pessoa";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Pessoa[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in pessoa of this model.
			 */
			public static async Task<Pessoa> createPessoa(Notificacao data, string id)
			{
				string APIPath = "notificacaos/:id/pessoa";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all pessoa of this model.
			 */
			public static async Task deletePessoa(string id)
			{
				string APIPath = "notificacaos/:id/pessoa";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts pessoa of notificacao.
			 */
			public static async Task<double> countPessoa(string id, string where = default(string))
			{
				string APIPath = "notificacaos/:id/pessoa/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Notificacao> patchOrCreate(Notificacao data)
			{
				string APIPath = "notificacaos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Notificacao>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Notificacao> replaceOrCreate(Notificacao data)
			{
				string APIPath = "notificacaos/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Notificacao>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<Notificacao> upsertWithWhere(Notificacao data, string where = default(string))
			{
				string APIPath = "notificacaos/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<Notificacao>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Notificacao> replaceById(Notificacao data, string id)
			{
				string APIPath = "notificacaos/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Notificacao>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Notificacao> patchAttributes(Notificacao data, string id)
			{
				string APIPath = "notificacaos/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Notificacao>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class Dispositivos : CRUDInterface<Dispositivo>
		{

			/*
			 * Fetches belongsTo relation pessoa.
			 */
			public static async Task<Pessoa> getPessoa(string id, bool refresh = default(bool))
			{
				string APIPath = "dispositivos/:id/pessoa";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Fetches belongsTo relation aplicativo.
			 */
			public static async Task<Aplicativo> getAplicativo(string id, bool refresh = default(bool))
			{
				string APIPath = "dispositivos/:id/aplicativo";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Aplicativo>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Dispositivo> patchOrCreate(Dispositivo data)
			{
				string APIPath = "dispositivos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Dispositivo>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Dispositivo> replaceOrCreate(Dispositivo data)
			{
				string APIPath = "dispositivos/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Dispositivo>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<Dispositivo> upsertWithWhere(Dispositivo data, string where = default(string))
			{
				string APIPath = "dispositivos/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<Dispositivo>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Dispositivo> replaceById(Dispositivo data, string id)
			{
				string APIPath = "dispositivos/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Dispositivo>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Dispositivo> patchAttributes(Dispositivo data, string id)
			{
				string APIPath = "dispositivos/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Dispositivo>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class PessoaRegistros : CRUDInterface<PessoaRegistro>
		{

			/*
			 * Fetches belongsTo relation pessoa.
			 */
			public static async Task<Pessoa> getPessoa(string id, bool refresh = default(bool))
			{
				string APIPath = "pessoaRegistros/:id/pessoa";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Fetches belongsTo relation usuario.
			 */
			public static async Task<Pessoa> getUsuario(string id, bool refresh = default(bool))
			{
				string APIPath = "pessoaRegistros/:id/usuario";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Fetches hasOne relation sinalVital.
			 */
			public static async Task<SinalVital> getSinalVital(string id, bool refresh = default(bool))
			{
				string APIPath = "pessoaRegistros/:id/sinalVital";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<SinalVital>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in sinalVital of this model.
			 */
			public static async Task<SinalVital> createSinalVital(PessoaRegistro data, string id)
			{
				string APIPath = "pessoaRegistros/:id/sinalVital";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<SinalVital>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update sinalVital of this model.
			 */
			public static async Task<SinalVital> updateSinalVital(PessoaRegistro data, string id)
			{
				string APIPath = "pessoaRegistros/:id/sinalVital";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<SinalVital>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes sinalVital of this model.
			 */
			public static async Task destroySinalVital(string id)
			{
				string APIPath = "pessoaRegistros/:id/sinalVital";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Fetches hasOne relation medicamento.
			 */
			public static async Task<PessoaMedicamento> getMedicamento(string id, bool refresh = default(bool))
			{
				string APIPath = "pessoaRegistros/:id/medicamento";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<PessoaMedicamento>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in medicamento of this model.
			 */
			public static async Task<PessoaMedicamento> createMedicamento(PessoaRegistro data, string id)
			{
				string APIPath = "pessoaRegistros/:id/medicamento";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<PessoaMedicamento>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update medicamento of this model.
			 */
			public static async Task<PessoaMedicamento> updateMedicamento(PessoaRegistro data, string id)
			{
				string APIPath = "pessoaRegistros/:id/medicamento";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<PessoaMedicamento>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes medicamento of this model.
			 */
			public static async Task destroyMedicamento(string id)
			{
				string APIPath = "pessoaRegistros/:id/medicamento";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<PessoaRegistro> patchOrCreate(PessoaRegistro data)
			{
				string APIPath = "pessoaRegistros";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<PessoaRegistro>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<PessoaRegistro> replaceOrCreate(PessoaRegistro data)
			{
				string APIPath = "pessoaRegistros/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<PessoaRegistro>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<PessoaRegistro> upsertWithWhere(PessoaRegistro data, string where = default(string))
			{
				string APIPath = "pessoaRegistros/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<PessoaRegistro>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<PessoaRegistro> replaceById(PessoaRegistro data, string id)
			{
				string APIPath = "pessoaRegistros/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<PessoaRegistro>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<PessoaRegistro> patchAttributes(PessoaRegistro data, string id)
			{
				string APIPath = "pessoaRegistros/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<PessoaRegistro>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class PessoaAlergias : CRUDInterface<PessoaAlergia>
		{

			/*
			 * Fetches belongsTo relation pessoa.
			 */
			public static async Task<Pessoa> getPessoa(string id, bool refresh = default(bool))
			{
				string APIPath = "pessoaAlergia/:id/pessoa";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<PessoaAlergia> patchOrCreate(PessoaAlergia data)
			{
				string APIPath = "pessoaAlergia";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<PessoaAlergia>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<PessoaAlergia> replaceOrCreate(PessoaAlergia data)
			{
				string APIPath = "pessoaAlergia/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<PessoaAlergia>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<PessoaAlergia> upsertWithWhere(PessoaAlergia data, string where = default(string))
			{
				string APIPath = "pessoaAlergia/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<PessoaAlergia>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<PessoaAlergia> replaceById(PessoaAlergia data, string id)
			{
				string APIPath = "pessoaAlergia/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<PessoaAlergia>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<PessoaAlergia> patchAttributes(PessoaAlergia data, string id)
			{
				string APIPath = "pessoaAlergia/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<PessoaAlergia>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class PessoaHistoricoFamiliars : CRUDInterface<PessoaHistoricoFamiliar>
		{

			/*
			 * Fetches belongsTo relation pessoa.
			 */
			public static async Task<Pessoa> getPessoa(string id, bool refresh = default(bool))
			{
				string APIPath = "pessoaHistoricoFamiliars/:id/pessoa";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<PessoaHistoricoFamiliar> patchOrCreate(PessoaHistoricoFamiliar data)
			{
				string APIPath = "pessoaHistoricoFamiliars";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<PessoaHistoricoFamiliar>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<PessoaHistoricoFamiliar> replaceOrCreate(PessoaHistoricoFamiliar data)
			{
				string APIPath = "pessoaHistoricoFamiliars/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<PessoaHistoricoFamiliar>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<PessoaHistoricoFamiliar> upsertWithWhere(PessoaHistoricoFamiliar data, string where = default(string))
			{
				string APIPath = "pessoaHistoricoFamiliars/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<PessoaHistoricoFamiliar>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<PessoaHistoricoFamiliar> replaceById(PessoaHistoricoFamiliar data, string id)
			{
				string APIPath = "pessoaHistoricoFamiliars/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<PessoaHistoricoFamiliar>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<PessoaHistoricoFamiliar> patchAttributes(PessoaHistoricoFamiliar data, string id)
			{
				string APIPath = "pessoaHistoricoFamiliars/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<PessoaHistoricoFamiliar>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class PessoaProtocolos : CRUDInterface<PessoaProtocolo>
		{

			/*
			 * Fetches belongsTo relation pessoa.
			 */
			public static async Task<Pessoa> getPessoa(string id, bool refresh = default(bool))
			{
				string APIPath = "pessoaProtocolos/:id/pessoa";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<PessoaProtocolo> patchOrCreate(PessoaProtocolo data)
			{
				string APIPath = "pessoaProtocolos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<PessoaProtocolo>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<PessoaProtocolo> replaceOrCreate(PessoaProtocolo data)
			{
				string APIPath = "pessoaProtocolos/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<PessoaProtocolo>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<PessoaProtocolo> upsertWithWhere(PessoaProtocolo data, string where = default(string))
			{
				string APIPath = "pessoaProtocolos/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<PessoaProtocolo>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<PessoaProtocolo> replaceById(PessoaProtocolo data, string id)
			{
				string APIPath = "pessoaProtocolos/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<PessoaProtocolo>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<PessoaProtocolo> patchAttributes(PessoaProtocolo data, string id)
			{
				string APIPath = "pessoaProtocolos/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<PessoaProtocolo>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class Protocolos : CRUDInterface<Protocolo>
		{

			/*
			 * Fetches belongsTo relation pessoa.
			 */
			public static async Task<Pessoa> getPessoa(string id, bool refresh = default(bool))
			{
				string APIPath = "protocolos/:id/pessoa";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Pessoa>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for detalheProtocolo.
			 */
			public static async Task<ProtocoloDetalhe> findByIdDetalheProtocolo(string id, string fk)
			{
				string APIPath = "protocolos/:id/detalheProtocolo/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<ProtocoloDetalhe>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for detalheProtocolo.
			 */
			public static async Task destroyByIdDetalheProtocolo(string id, string fk)
			{
				string APIPath = "protocolos/:id/detalheProtocolo/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for detalheProtocolo.
			 */
			public static async Task<ProtocoloDetalhe> updateByIdDetalheProtocolo(Protocolo data, string id, string fk)
			{
				string APIPath = "protocolos/:id/detalheProtocolo/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<ProtocoloDetalhe>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries detalheProtocolo of protocolo.
			 */
			public static async Task<IList<ProtocoloDetalhe>> getDetalheProtocolo(string id, string filter = default(string))
			{
				string APIPath = "protocolos/:id/detalheProtocolo";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<ProtocoloDetalhe[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in detalheProtocolo of this model.
			 */
			public static async Task<ProtocoloDetalhe> createDetalheProtocolo(Protocolo data, string id)
			{
				string APIPath = "protocolos/:id/detalheProtocolo";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<ProtocoloDetalhe>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all detalheProtocolo of this model.
			 */
			public static async Task deleteDetalheProtocolo(string id)
			{
				string APIPath = "protocolos/:id/detalheProtocolo";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts detalheProtocolo of protocolo.
			 */
			public static async Task<double> countDetalheProtocolo(string id, string where = default(string))
			{
				string APIPath = "protocolos/:id/detalheProtocolo/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Protocolo> patchOrCreate(Protocolo data)
			{
				string APIPath = "protocolos";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Protocolo>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<Protocolo> replaceOrCreate(Protocolo data)
			{
				string APIPath = "protocolos/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Protocolo>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<Protocolo> upsertWithWhere(Protocolo data, string where = default(string))
			{
				string APIPath = "protocolos/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<Protocolo>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Protocolo> replaceById(Protocolo data, string id)
			{
				string APIPath = "protocolos/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Protocolo>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<Protocolo> patchAttributes(Protocolo data, string id)
			{
				string APIPath = "protocolos/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Protocolo>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class ProtocoloDetalhes : CRUDInterface<ProtocoloDetalhe>
		{

			/*
			 * Find a related item by id for detalheProtocolo.
			 */
			public static async Task<ProtocoloDetalhe> findByIdForprotocolo(string id, string fk)
			{
				string APIPath = "protocolos/:id/detalheProtocolo/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<ProtocoloDetalhe>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for detalheProtocolo.
			 */
			public static async Task destroyByIdForprotocolo(string id, string fk)
			{
				string APIPath = "protocolos/:id/detalheProtocolo/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for detalheProtocolo.
			 */
			public static async Task<ProtocoloDetalhe> updateByIdForprotocolo(ProtocoloDetalhe data, string id, string fk)
			{
				string APIPath = "protocolos/:id/detalheProtocolo/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<ProtocoloDetalhe>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries detalheProtocolo of protocolo.
			 */
			public static async Task<IList<ProtocoloDetalhe>> getForprotocolo(string id, string filter = default(string))
			{
				string APIPath = "protocolos/:id/detalheProtocolo";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<ProtocoloDetalhe[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in detalheProtocolo of this model.
			 */
			public static async Task<ProtocoloDetalhe> createForprotocolo(ProtocoloDetalhe data, string id)
			{
				string APIPath = "protocolos/:id/detalheProtocolo";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<ProtocoloDetalhe>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all detalheProtocolo of this model.
			 */
			public static async Task deleteForprotocolo(string id)
			{
				string APIPath = "protocolos/:id/detalheProtocolo";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts detalheProtocolo of protocolo.
			 */
			public static async Task<double> countForprotocolo(string id, string where = default(string))
			{
				string APIPath = "protocolos/:id/detalheProtocolo/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}
		}
		public class SinaisVitais : CRUDInterface<SinalVital>
		{

			/*
			 * Fetches hasOne relation sinalVital.
			 */
			public static async Task<SinalVital> getForpessoaRegistro(string id, bool refresh = default(bool))
			{
				string APIPath = "pessoaRegistros/:id/sinalVital";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<SinalVital>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in sinalVital of this model.
			 */
			public static async Task<SinalVital> createForpessoaRegistro(SinalVital data, string id)
			{
				string APIPath = "pessoaRegistros/:id/sinalVital";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<SinalVital>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update sinalVital of this model.
			 */
			public static async Task<SinalVital> updateForpessoaRegistro(SinalVital data, string id)
			{
				string APIPath = "pessoaRegistros/:id/sinalVital";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<SinalVital>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes sinalVital of this model.
			 */
			public static async Task destroyForpessoaRegistro(string id)
			{
				string APIPath = "pessoaRegistros/:id/sinalVital";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
		}
		public class PessoaMedicamentos : CRUDInterface<PessoaMedicamento>
		{

			/*
			 * Fetches hasOne relation medicamento.
			 */
			public static async Task<PessoaMedicamento> getForpessoaRegistro(string id, bool refresh = default(bool))
			{
				string APIPath = "pessoaRegistros/:id/medicamento";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<PessoaMedicamento>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in medicamento of this model.
			 */
			public static async Task<PessoaMedicamento> createForpessoaRegistro(PessoaMedicamento data, string id)
			{
				string APIPath = "pessoaRegistros/:id/medicamento";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<PessoaMedicamento>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update medicamento of this model.
			 */
			public static async Task<PessoaMedicamento> updateForpessoaRegistro(PessoaMedicamento data, string id)
			{
				string APIPath = "pessoaRegistros/:id/medicamento";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<PessoaMedicamento>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes medicamento of this model.
			 */
			public static async Task destroyForpessoaRegistro(string id)
			{
				string APIPath = "pessoaRegistros/:id/medicamento";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
		}
		public class CadastroPessoas : CRUDInterface<CadastroPessoa>
		{

			/*
			 * Patch an existing model instance or insert a new one into the data source.
			 */
			public static async Task<CadastroPessoa> patchOrCreate(CadastroPessoa data)
			{
				string APIPath = "cadastroPessoas";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<CadastroPessoa>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace an existing model instance or insert a new one into the data source.
			 */
			public static async Task<CadastroPessoa> replaceOrCreate(CadastroPessoa data)
			{
				string APIPath = "cadastroPessoas/replaceOrCreate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<CadastroPessoa>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update an existing model instance or insert a new one into the data source based on the where criteria.
			 */
			public static async Task<CadastroPessoa> upsertWithWhere(CadastroPessoa data, string where = default(string))
			{
				string APIPath = "cadastroPessoas/upsertWithWhere";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<CadastroPessoa>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Replace attributes for a model instance and persist it into the data source.
			 */
			public static async Task<CadastroPessoa> replaceById(CadastroPessoa data, string id)
			{
				string APIPath = "cadastroPessoas/:id/replace";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<CadastroPessoa>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Patch attributes for a model instance and persist it into the data source.
			 */
			public static async Task<CadastroPessoa> patchAttributes(CadastroPessoa data, string id)
			{
				string APIPath = "cadastroPessoas/:id";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<CadastroPessoa>(APIPath, bodyJSON, "PATCH", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class ExameDetalhes : CRUDInterface<ExameDetalhe>
		{

			/*
			 * Find a related item by id for detalhePreparo.
			 */
			public static async Task<ExameDetalhe> findByIdForexamePreparo(string id, string fk)
			{
				string APIPath = "examePreparos/:id/detalhePreparo/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<ExameDetalhe>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for detalhePreparo.
			 */
			public static async Task destroyByIdForexamePreparo(string id, string fk)
			{
				string APIPath = "examePreparos/:id/detalhePreparo/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for detalhePreparo.
			 */
			public static async Task<ExameDetalhe> updateByIdForexamePreparo(ExameDetalhe data, string id, string fk)
			{
				string APIPath = "examePreparos/:id/detalhePreparo/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<ExameDetalhe>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries detalhePreparo of examePreparo.
			 */
			public static async Task<IList<ExameDetalhe>> getForexamePreparo(string id, string filter = default(string))
			{
				string APIPath = "examePreparos/:id/detalhePreparo";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<ExameDetalhe[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in detalhePreparo of this model.
			 */
			public static async Task<ExameDetalhe> createForexamePreparo(ExameDetalhe data, string id)
			{
				string APIPath = "examePreparos/:id/detalhePreparo";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<ExameDetalhe>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all detalhePreparo of this model.
			 */
			public static async Task deleteForexamePreparo(string id)
			{
				string APIPath = "examePreparos/:id/detalhePreparo";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts detalhePreparo of examePreparo.
			 */
			public static async Task<double> countForexamePreparo(string id, string where = default(string))
			{
				string APIPath = "examePreparos/:id/detalhePreparo/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return JObject.Parse(response.ToString()).First.First.ToObject<double>();
			}
		}
		
	}
}

/**
 *** Dynamic Models ***
 */

namespace LBXamarinSDK
{
	public partial class Email : LBModel
	{
		[JsonIgnore] 
						private String _to;			
				
		[JsonProperty ("to", NullValueHandling = NullValueHandling.Ignore)]
	public String to
		{
			get { return _to ; }//não primitivo
			set { SetProperty (ref  _to , value); }
		}
			[JsonIgnore] 
						private String _from;			
				
		[JsonProperty ("from", NullValueHandling = NullValueHandling.Ignore)]
	public String from
		{
			get { return _from ; }//não primitivo
			set { SetProperty (ref  _from , value); }
		}
			[JsonIgnore] 
						private String _subject;			
				
		[JsonProperty ("subject", NullValueHandling = NullValueHandling.Ignore)]
	public String subject
		{
			get { return _subject ; }//não primitivo
			set { SetProperty (ref  _subject , value); }
		}
			[JsonIgnore] 
						private String _text;			
				
		[JsonProperty ("text", NullValueHandling = NullValueHandling.Ignore)]
	public String text
		{
			get { return _text ; }//não primitivo
			set { SetProperty (ref  _text , value); }
		}
			[JsonIgnore] 
						private String _html;			
				
		[JsonProperty ("html", NullValueHandling = NullValueHandling.Ignore)]
	public String html
		{
			get { return _html ; }//não primitivo
			set { SetProperty (ref  _html , value); }
		}
			[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class User : LBModel
	{
		[JsonIgnore] 
						private String _realm;			
				
		[JsonProperty ("realm", NullValueHandling = NullValueHandling.Ignore)]
	public String realm
		{
			get { return _realm ; }//não primitivo
			set { SetProperty (ref  _realm , value); }
		}
			[JsonIgnore] 
						private String _username;			
				
		[JsonProperty ("username", NullValueHandling = NullValueHandling.Ignore)]
	public String username
		{
			get { return _username ; }//não primitivo
			set { SetProperty (ref  _username , value); }
		}
			[JsonIgnore] 
						private String _password;			
				
		[JsonProperty ("password", NullValueHandling = NullValueHandling.Ignore)]
	public String password
		{
			get { return _password ; }//não primitivo
			set { SetProperty (ref  _password , value); }
		}
			[JsonIgnore] 
						private String _email;			
				
		[JsonProperty ("email", NullValueHandling = NullValueHandling.Ignore)]
	public String email
		{
			get { return _email ; }//não primitivo
			set { SetProperty (ref  _email , value); }
		}
			[JsonIgnore] 
						private bool _emailVerified;			
				
		[JsonProperty ("emailVerified", NullValueHandling = NullValueHandling.Ignore)]
	public bool emailVerified
		{
			get { return _emailVerified; } //primitivo
		set { SetProperty (ref  _emailVerified , value); }
		}
			[JsonIgnore] 
						private String _verificationToken;			
				
		[JsonProperty ("verificationToken", NullValueHandling = NullValueHandling.Ignore)]
	public String verificationToken
		{
			get { return _verificationToken ; }//não primitivo
			set { SetProperty (ref  _verificationToken , value); }
		}
			[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class AccessToken : LBModel
	{
		[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			[JsonIgnore] 
						private double _ttl;			
				
		[JsonProperty ("ttl", NullValueHandling = NullValueHandling.Ignore)]
	public double ttl
		{
			get { return _ttl; } //primitivo
		set { SetProperty (ref  _ttl , value); }
		}
			[JsonIgnore] 
								
	private ObservableCollection<String> _scopes = new ObservableCollection<String>();			
		[JsonProperty ("scopes", NullValueHandling = NullValueHandling.Ignore)]
		public ObservableCollection<String> scopes
		{
			get { return _scopes; }
			set { _scopes = value; }
		}
				[JsonIgnore] 
						private DateTime _created;			
				
		[JsonProperty ("created", NullValueHandling = NullValueHandling.Ignore)]
	public DateTime created
		{
			get { return _created; } //primitivo
		set { SetProperty (ref  _created , value); }
		}
			[JsonIgnore] 
						private String _userId;			
				
		[JsonProperty ("userId", NullValueHandling = NullValueHandling.Ignore)]
	public String userId
		{
			get { return _userId ; }//não primitivo
			set { SetProperty (ref  _userId , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class UserCredential : LBModel
	{
		[JsonIgnore] 
						private String _provider;			
				
		[JsonProperty ("provider", NullValueHandling = NullValueHandling.Ignore)]
	public String provider
		{
			get { return _provider ; }//não primitivo
			set { SetProperty (ref  _provider , value); }
		}
			[JsonIgnore] 
						private String _authScheme;			
				
		[JsonProperty ("authScheme", NullValueHandling = NullValueHandling.Ignore)]
	public String authScheme
		{
			get { return _authScheme ; }//não primitivo
			set { SetProperty (ref  _authScheme , value); }
		}
			[JsonIgnore] 
						private String _externalId;			
				
		[JsonProperty ("externalId", NullValueHandling = NullValueHandling.Ignore)]
	public String externalId
		{
			get { return _externalId ; }//não primitivo
			set { SetProperty (ref  _externalId , value); }
		}
			[JsonIgnore] 
						private Object _profile;			
				
		[JsonProperty ("profile", NullValueHandling = NullValueHandling.Ignore)]
	public Object profile
		{
			get { return _profile ; }//não primitivo
			set { SetProperty (ref  _profile , value); }
		}
			[JsonIgnore] 
						private Object _credentials;			
				
		[JsonProperty ("credentials", NullValueHandling = NullValueHandling.Ignore)]
	public Object credentials
		{
			get { return _credentials ; }//não primitivo
			set { SetProperty (ref  _credentials , value); }
		}
			[JsonIgnore] 
						private DateTime _created;			
				
		[JsonProperty ("created", NullValueHandling = NullValueHandling.Ignore)]
	public DateTime created
		{
			get { return _created; } //primitivo
		set { SetProperty (ref  _created , value); }
		}
			[JsonIgnore] 
						private DateTime _modified;			
				
		[JsonProperty ("modified", NullValueHandling = NullValueHandling.Ignore)]
	public DateTime modified
		{
			get { return _modified; } //primitivo
		set { SetProperty (ref  _modified , value); }
		}
			[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			[JsonIgnore] 
						private String _userId;			
				
		[JsonProperty ("userId", NullValueHandling = NullValueHandling.Ignore)]
	public String userId
		{
			get { return _userId ; }//não primitivo
			set { SetProperty (ref  _userId , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class UserIdentity : LBModel
	{
		[JsonIgnore] 
						private String _provider;			
				
		[JsonProperty ("provider", NullValueHandling = NullValueHandling.Ignore)]
	public String provider
		{
			get { return _provider ; }//não primitivo
			set { SetProperty (ref  _provider , value); }
		}
			[JsonIgnore] 
						private String _authScheme;			
				
		[JsonProperty ("authScheme", NullValueHandling = NullValueHandling.Ignore)]
	public String authScheme
		{
			get { return _authScheme ; }//não primitivo
			set { SetProperty (ref  _authScheme , value); }
		}
			[JsonIgnore] 
						private String _externalId;			
				
		[JsonProperty ("externalId", NullValueHandling = NullValueHandling.Ignore)]
	public String externalId
		{
			get { return _externalId ; }//não primitivo
			set { SetProperty (ref  _externalId , value); }
		}
			[JsonIgnore] 
						private Object _profile;			
				
		[JsonProperty ("profile", NullValueHandling = NullValueHandling.Ignore)]
	public Object profile
		{
			get { return _profile ; }//não primitivo
			set { SetProperty (ref  _profile , value); }
		}
			[JsonIgnore] 
						private Object _credentials;			
				
		[JsonProperty ("credentials", NullValueHandling = NullValueHandling.Ignore)]
	public Object credentials
		{
			get { return _credentials ; }//não primitivo
			set { SetProperty (ref  _credentials , value); }
		}
			[JsonIgnore] 
						private DateTime _created;			
				
		[JsonProperty ("created", NullValueHandling = NullValueHandling.Ignore)]
	public DateTime created
		{
			get { return _created; } //primitivo
		set { SetProperty (ref  _created , value); }
		}
			[JsonIgnore] 
						private DateTime _modified;			
				
		[JsonProperty ("modified", NullValueHandling = NullValueHandling.Ignore)]
	public DateTime modified
		{
			get { return _modified; } //primitivo
		set { SetProperty (ref  _modified , value); }
		}
			[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			[JsonIgnore] 
						private String _userId;			
				
		[JsonProperty ("userId", NullValueHandling = NullValueHandling.Ignore)]
	public String userId
		{
			get { return _userId ; }//não primitivo
			set { SetProperty (ref  _userId , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class ACL : LBModel
	{
		[JsonIgnore] 
						private String _model;			
				
		[JsonProperty ("model", NullValueHandling = NullValueHandling.Ignore)]
	public String model
		{
			get { return _model ; }//não primitivo
			set { SetProperty (ref  _model , value); }
		}
			[JsonIgnore] 
						private String _property;			
				
		[JsonProperty ("property", NullValueHandling = NullValueHandling.Ignore)]
	public String property
		{
			get { return _property ; }//não primitivo
			set { SetProperty (ref  _property , value); }
		}
			[JsonIgnore] 
						private String _accessType;			
				
		[JsonProperty ("accessType", NullValueHandling = NullValueHandling.Ignore)]
	public String accessType
		{
			get { return _accessType ; }//não primitivo
			set { SetProperty (ref  _accessType , value); }
		}
			[JsonIgnore] 
						private String _permission;			
				
		[JsonProperty ("permission", NullValueHandling = NullValueHandling.Ignore)]
	public String permission
		{
			get { return _permission ; }//não primitivo
			set { SetProperty (ref  _permission , value); }
		}
			[JsonIgnore] 
						private String _principalType;			
				
		[JsonProperty ("principalType", NullValueHandling = NullValueHandling.Ignore)]
	public String principalType
		{
			get { return _principalType ; }//não primitivo
			set { SetProperty (ref  _principalType , value); }
		}
			[JsonIgnore] 
						private String _principalId;			
				
		[JsonProperty ("principalId", NullValueHandling = NullValueHandling.Ignore)]
	public String principalId
		{
			get { return _principalId ; }//não primitivo
			set { SetProperty (ref  _principalId , value); }
		}
			[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class RoleMapping : LBModel
	{
		[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			[JsonIgnore] 
						private String _principalType;			
				
		[JsonProperty ("principalType", NullValueHandling = NullValueHandling.Ignore)]
	public String principalType
		{
			get { return _principalType ; }//não primitivo
			set { SetProperty (ref  _principalType , value); }
		}
			[JsonIgnore] 
						private String _principalId;			
				
		[JsonProperty ("principalId", NullValueHandling = NullValueHandling.Ignore)]
	public String principalId
		{
			get { return _principalId ; }//não primitivo
			set { SetProperty (ref  _principalId , value); }
		}
			[JsonIgnore] 
						private String _roleId;			
				
		[JsonProperty ("roleId", NullValueHandling = NullValueHandling.Ignore)]
	public String roleId
		{
			get { return _roleId ; }//não primitivo
			set { SetProperty (ref  _roleId , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class Role : LBModel
	{
		[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			[JsonIgnore] 
						private String _name;			
				
		[JsonProperty ("name", NullValueHandling = NullValueHandling.Ignore)]
	public String name
		{
			get { return _name ; }//não primitivo
			set { SetProperty (ref  _name , value); }
		}
			[JsonIgnore] 
						private String _description;			
				
		[JsonProperty ("description", NullValueHandling = NullValueHandling.Ignore)]
	public String description
		{
			get { return _description ; }//não primitivo
			set { SetProperty (ref  _description , value); }
		}
			[JsonIgnore] 
						private DateTime _created;			
				
		[JsonProperty ("created", NullValueHandling = NullValueHandling.Ignore)]
	public DateTime created
		{
			get { return _created; } //primitivo
		set { SetProperty (ref  _created , value); }
		}
			[JsonIgnore] 
						private DateTime _modified;			
				
		[JsonProperty ("modified", NullValueHandling = NullValueHandling.Ignore)]
	public DateTime modified
		{
			get { return _modified; } //primitivo
		set { SetProperty (ref  _modified , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class Enumerado : LBModel
	{
		[JsonIgnore] 
						private String _nome;			
				
		[JsonProperty ("nome", NullValueHandling = NullValueHandling.Ignore)]
	public String nome
		{
			get { return _nome ; }//não primitivo
			set { SetProperty (ref  _nome , value); }
		}
			[JsonIgnore] 
						private String _codigoEnu;			
				
		[JsonProperty ("codigoEnu", NullValueHandling = NullValueHandling.Ignore)]
	public String codigoEnu
		{
			get { return _codigoEnu ; }//não primitivo
			set { SetProperty (ref  _codigoEnu , value); }
		}
			[JsonIgnore] 
						private String _descricaoEnu;			
				
		[JsonProperty ("descricaoEnu", NullValueHandling = NullValueHandling.Ignore)]
	public String descricaoEnu
		{
			get { return _descricaoEnu ; }//não primitivo
			set { SetProperty (ref  _descricaoEnu , value); }
		}
			[JsonIgnore] 
						private double _sequencia;			
				
		[JsonProperty ("sequencia", NullValueHandling = NullValueHandling.Ignore)]
	public double sequencia
		{
			get { return _sequencia; } //primitivo
		set { SetProperty (ref  _sequencia , value); }
		}
			[JsonIgnore] 
						private bool _inativo;			
				
		[JsonProperty ("inativo", NullValueHandling = NullValueHandling.Ignore)]
	public bool inativo
		{
			get { return _inativo; } //primitivo
		set { SetProperty (ref  _inativo , value); }
		}
			[JsonIgnore] 
						private Object _detalheEnu;			
				
		[JsonProperty ("detalheEnu", NullValueHandling = NullValueHandling.Ignore)]
	public Object detalheEnu
		{
			get { return _detalheEnu ; }//não primitivo
			set { SetProperty (ref  _detalheEnu , value); }
		}
			[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			[JsonIgnore] 
								
	private ObservableCollection<String> _aplicativoId = new ObservableCollection<String>();			
		[JsonProperty ("aplicativoId", NullValueHandling = NullValueHandling.Ignore)]
		public ObservableCollection<String> aplicativoId
		{
			get { return _aplicativoId; }
			set { _aplicativoId = value; }
		}
				
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class Parametro : LBModel
	{
		[JsonIgnore] 
						private string _codParametro;			
				
		[JsonProperty ("codParametro", NullValueHandling = NullValueHandling.Ignore)]
	public string codParametro
		{
			get { return _codParametro ; }//não primitivo
			set { SetProperty (ref  _codParametro , value); }
		}
			[JsonIgnore] 
						private String _descricao;			
				
		[JsonProperty ("descricao", NullValueHandling = NullValueHandling.Ignore)]
	public String descricao
		{
			get { return _descricao ; }//não primitivo
			set { SetProperty (ref  _descricao , value); }
		}
			[JsonIgnore] 
						private String _valParametro;			
				
		[JsonProperty ("valParametro", NullValueHandling = NullValueHandling.Ignore)]
	public String valParametro
		{
			get { return _valParametro ; }//não primitivo
			set { SetProperty (ref  _valParametro , value); }
		}
			[JsonIgnore] 
						private Object _detalhe;			
				
		[JsonProperty ("detalhe", NullValueHandling = NullValueHandling.Ignore)]
	public Object detalhe
		{
			get { return _detalhe ; }//não primitivo
			set { SetProperty (ref  _detalhe , value); }
		}
			[JsonIgnore] 
						private bool _inativo;			
				
		[JsonProperty ("inativo", NullValueHandling = NullValueHandling.Ignore)]
	public bool inativo
		{
			get { return _inativo; } //primitivo
		set { SetProperty (ref  _inativo , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return codParametro;
		}
	}
	public partial class Job : LBModel
	{
		[JsonIgnore] 
						private String _nomeJob;			
				
		[JsonProperty ("nomeJob", NullValueHandling = NullValueHandling.Ignore)]
	public String nomeJob
		{
			get { return _nomeJob ; }//não primitivo
			set { SetProperty (ref  _nomeJob , value); }
		}
			[JsonIgnore] 
						private DateTime _dtCriacao;			
				
		[JsonProperty ("dtCriacao", NullValueHandling = NullValueHandling.Ignore)]
	public DateTime dtCriacao
		{
			get { return _dtCriacao; } //primitivo
		set { SetProperty (ref  _dtCriacao , value); }
		}
			[JsonIgnore] 
						private Object _parametro;			
				
		[JsonProperty ("parametro", NullValueHandling = NullValueHandling.Ignore)]
	public Object parametro
		{
			get { return _parametro ; }//não primitivo
			set { SetProperty (ref  _parametro , value); }
		}
			[JsonIgnore] 
						private Object _resultado;			
				
		[JsonProperty ("resultado", NullValueHandling = NullValueHandling.Ignore)]
	public Object resultado
		{
			get { return _resultado ; }//não primitivo
			set { SetProperty (ref  _resultado , value); }
		}
			[JsonIgnore] 
						private double _status;			
				
		[JsonProperty ("status", NullValueHandling = NullValueHandling.Ignore)]
	public double status
		{
			get { return _status; } //primitivo
		set { SetProperty (ref  _status , value); }
		}
			[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class Pais : LBModel
	{
		[JsonIgnore] 
						private String _nome;			
				
		[JsonProperty ("nome", NullValueHandling = NullValueHandling.Ignore)]
	public String nome
		{
			get { return _nome ; }//não primitivo
			set { SetProperty (ref  _nome , value); }
		}
			[JsonIgnore] 
						private String _codigoDDI;			
				
		[JsonProperty ("codigoDDI", NullValueHandling = NullValueHandling.Ignore)]
	public String codigoDDI
		{
			get { return _codigoDDI ; }//não primitivo
			set { SetProperty (ref  _codigoDDI , value); }
		}
			[JsonIgnore] 
						private String _imagemBandeira;			
				
		[JsonProperty ("imagemBandeira", NullValueHandling = NullValueHandling.Ignore)]
	public String imagemBandeira
		{
			get { return _imagemBandeira ; }//não primitivo
			set { SetProperty (ref  _imagemBandeira , value); }
		}
			[JsonIgnore] 
						private String _locale;			
				
		[JsonProperty ("locale", NullValueHandling = NullValueHandling.Ignore)]
	public String locale
		{
			get { return _locale ; }//não primitivo
			set { SetProperty (ref  _locale , value); }
		}
			[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class TextoPadrao : LBModel
	{
		[JsonIgnore] 
						private double _tipo;			
				
		[JsonProperty ("tipo", NullValueHandling = NullValueHandling.Ignore)]
	public double tipo
		{
			get { return _tipo; } //primitivo
		set { SetProperty (ref  _tipo , value); }
		}
			[JsonIgnore] 
						private String _nome;			
				
		[JsonProperty ("nome", NullValueHandling = NullValueHandling.Ignore)]
	public String nome
		{
			get { return _nome ; }//não primitivo
			set { SetProperty (ref  _nome , value); }
		}
			[JsonIgnore] 
						private String _conteudo;			
				
		[JsonProperty ("conteudo", NullValueHandling = NullValueHandling.Ignore)]
	public String conteudo
		{
			get { return _conteudo ; }//não primitivo
			set { SetProperty (ref  _conteudo , value); }
		}
			[JsonIgnore] 
						private double _inativo;			
				
		[JsonProperty ("inativo", NullValueHandling = NullValueHandling.Ignore)]
	public double inativo
		{
			get { return _inativo; } //primitivo
		set { SetProperty (ref  _inativo , value); }
		}
			[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class Pessoa : LBModel
	{
		[JsonIgnore] 
						private String _nomeCompleto;			
				
		[JsonProperty ("nomeCompleto", NullValueHandling = NullValueHandling.Ignore)]
	public String nomeCompleto
		{
			get { return _nomeCompleto ; }//não primitivo
			set { SetProperty (ref  _nomeCompleto , value); }
		}
			[JsonIgnore] 
						private DateTime _dataNascimento;			
				
		[JsonProperty ("dataNascimento", NullValueHandling = NullValueHandling.Ignore)]
	public DateTime dataNascimento
		{
			get { return _dataNascimento; } //primitivo
		set { SetProperty (ref  _dataNascimento , value); }
		}
			[JsonIgnore] 
						private Object _infoAd;			
				
		[JsonProperty ("infoAd", NullValueHandling = NullValueHandling.Ignore)]
	public Object infoAd
		{
			get { return _infoAd ; }//não primitivo
			set { SetProperty (ref  _infoAd , value); }
		}
			[JsonIgnore] 
						private String _email;			
				
		[JsonProperty ("email", NullValueHandling = NullValueHandling.Ignore)]
	public String email
		{
			get { return _email ; }//não primitivo
			set { SetProperty (ref  _email , value); }
		}
			[JsonIgnore] 
						private DateTime _dtCadastro;			
				
		[JsonProperty ("dtCadastro", NullValueHandling = NullValueHandling.Ignore)]
	public DateTime dtCadastro
		{
			get { return _dtCadastro; } //primitivo
		set { SetProperty (ref  _dtCadastro , value); }
		}
			[JsonIgnore] 
						private String _genero;			
				
		[JsonProperty ("genero", NullValueHandling = NullValueHandling.Ignore)]
	public String genero
		{
			get { return _genero ; }//não primitivo
			set { SetProperty (ref  _genero , value); }
		}
			[JsonIgnore] 
						private String _nrCPF;			
				
		[JsonProperty ("nrCPF", NullValueHandling = NullValueHandling.Ignore)]
	public String nrCPF
		{
			get { return _nrCPF ; }//não primitivo
			set { SetProperty (ref  _nrCPF , value); }
		}
			[JsonIgnore] 
								
	private ObservableCollection<String> _listPerfilAcesso = new ObservableCollection<String>();			
		[JsonProperty ("listPerfilAcesso", NullValueHandling = NullValueHandling.Ignore)]
		public ObservableCollection<String> listPerfilAcesso
		{
			get { return _listPerfilAcesso; }
			set { _listPerfilAcesso = value; }
		}
				[JsonIgnore] 
						private String _tipoSanguineo;			
				
		[JsonProperty ("tipoSanguineo", NullValueHandling = NullValueHandling.Ignore)]
	public String tipoSanguineo
		{
			get { return _tipoSanguineo ; }//não primitivo
			set { SetProperty (ref  _tipoSanguineo , value); }
		}
			[JsonIgnore] 
						private String _imgPerfil;			
				
		[JsonProperty ("imgPerfil", NullValueHandling = NullValueHandling.Ignore)]
	public String imgPerfil
		{
			get { return _imgPerfil ; }//não primitivo
			set { SetProperty (ref  _imgPerfil , value); }
		}
			[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			[JsonIgnore] 
								
	private ObservableCollection<String> _userId = new ObservableCollection<String>();			
		[JsonProperty ("userId", NullValueHandling = NullValueHandling.Ignore)]
		public ObservableCollection<String> userId
		{
			get { return _userId; }
			set { _userId = value; }
		}
				
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class PerfilAcesso : LBModel
	{
		[JsonIgnore] 
						private string _codigoPerfil;			
				
		[JsonProperty ("codigoPerfil", NullValueHandling = NullValueHandling.Ignore)]
	public string codigoPerfil
		{
			get { return _codigoPerfil ; }//não primitivo
			set { SetProperty (ref  _codigoPerfil , value); }
		}
			[JsonIgnore] 
						private String _descricao;			
				
		[JsonProperty ("descricao", NullValueHandling = NullValueHandling.Ignore)]
	public String descricao
		{
			get { return _descricao ; }//não primitivo
			set { SetProperty (ref  _descricao , value); }
		}
			[JsonIgnore] 
						private double _sequencia;			
				
		[JsonProperty ("sequencia", NullValueHandling = NullValueHandling.Ignore)]
	public double sequencia
		{
			get { return _sequencia; } //primitivo
		set { SetProperty (ref  _sequencia , value); }
		}
			[JsonIgnore] 
						private bool _inativo;			
				
		[JsonProperty ("inativo", NullValueHandling = NullValueHandling.Ignore)]
	public bool inativo
		{
			get { return _inativo; } //primitivo
		set { SetProperty (ref  _inativo , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return codigoPerfil;
		}
	}
	public partial class Rota : LBModel
	{
		[JsonIgnore] 
						private string _state;			
				
		[JsonProperty ("state", NullValueHandling = NullValueHandling.Ignore)]
	public string state
		{
			get { return _state ; }//não primitivo
			set { SetProperty (ref  _state , value); }
		}
			[JsonIgnore] 
						private String _titulo;			
				
		[JsonProperty ("titulo", NullValueHandling = NullValueHandling.Ignore)]
	public String titulo
		{
			get { return _titulo ; }//não primitivo
			set { SetProperty (ref  _titulo , value); }
		}
			[JsonIgnore] 
								
	private ObservableCollection<String> _listPerfilAcesso = new ObservableCollection<String>();			
		[JsonProperty ("listPerfilAcesso", NullValueHandling = NullValueHandling.Ignore)]
		public ObservableCollection<String> listPerfilAcesso
		{
			get { return _listPerfilAcesso; }
			set { _listPerfilAcesso = value; }
		}
				
		// This method identifies the ID field
		public override string getID()
		{
			return state;
		}
	}
	public partial class Periodicidade : LBModel
	{
		[JsonIgnore] 
						private String _nome;			
				
		[JsonProperty ("nome", NullValueHandling = NullValueHandling.Ignore)]
	public String nome
		{
			get { return _nome ; }//não primitivo
			set { SetProperty (ref  _nome , value); }
		}
			[JsonIgnore] 
						private String _descricao;			
				
		[JsonProperty ("descricao", NullValueHandling = NullValueHandling.Ignore)]
	public String descricao
		{
			get { return _descricao ; }//não primitivo
			set { SetProperty (ref  _descricao , value); }
		}
			[JsonIgnore] 
						private bool _ativo;			
				
		[JsonProperty ("ativo", NullValueHandling = NullValueHandling.Ignore)]
	public bool ativo
		{
			get { return _ativo; } //primitivo
		set { SetProperty (ref  _ativo , value); }
		}
			[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class TipoServico : LBModel
	{
		[JsonIgnore] 
						private String _nome;			
				
		[JsonProperty ("nome", NullValueHandling = NullValueHandling.Ignore)]
	public String nome
		{
			get { return _nome ; }//não primitivo
			set { SetProperty (ref  _nome , value); }
		}
			[JsonIgnore] 
						private String _descricao;			
				
		[JsonProperty ("descricao", NullValueHandling = NullValueHandling.Ignore)]
	public String descricao
		{
			get { return _descricao ; }//não primitivo
			set { SetProperty (ref  _descricao , value); }
		}
			[JsonIgnore] 
						private bool _ativo;			
				
		[JsonProperty ("ativo", NullValueHandling = NullValueHandling.Ignore)]
	public bool ativo
		{
			get { return _ativo; } //primitivo
		set { SetProperty (ref  _ativo , value); }
		}
			[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class Produto : LBModel
	{
		[JsonIgnore] 
						private String _nome;			
				
		[JsonProperty ("nome", NullValueHandling = NullValueHandling.Ignore)]
	public String nome
		{
			get { return _nome ; }//não primitivo
			set { SetProperty (ref  _nome , value); }
		}
			[JsonIgnore] 
						private double _preco;			
				
		[JsonProperty ("preco", NullValueHandling = NullValueHandling.Ignore)]
	public double preco
		{
			get { return _preco; } //primitivo
		set { SetProperty (ref  _preco , value); }
		}
			[JsonIgnore] 
						private String _textoServico;			
				
		[JsonProperty ("textoServico", NullValueHandling = NullValueHandling.Ignore)]
	public String textoServico
		{
			get { return _textoServico ; }//não primitivo
			set { SetProperty (ref  _textoServico , value); }
		}
			[JsonIgnore] 
						private String _textoDetalhamento;			
				
		[JsonProperty ("textoDetalhamento", NullValueHandling = NullValueHandling.Ignore)]
	public String textoDetalhamento
		{
			get { return _textoDetalhamento ; }//não primitivo
			set { SetProperty (ref  _textoDetalhamento , value); }
		}
			[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class Dica : LBModel
	{
		[JsonIgnore] 
						private String _titulo;			
				
		[JsonProperty ("titulo", NullValueHandling = NullValueHandling.Ignore)]
	public String titulo
		{
			get { return _titulo ; }//não primitivo
			set { SetProperty (ref  _titulo , value); }
		}
			[JsonIgnore] 
						private String _subTitulo;			
				
		[JsonProperty ("subTitulo", NullValueHandling = NullValueHandling.Ignore)]
	public String subTitulo
		{
			get { return _subTitulo ; }//não primitivo
			set { SetProperty (ref  _subTitulo , value); }
		}
			[JsonIgnore] 
						private String _imagem;			
				
		[JsonProperty ("imagem", NullValueHandling = NullValueHandling.Ignore)]
	public String imagem
		{
			get { return _imagem ; }//não primitivo
			set { SetProperty (ref  _imagem , value); }
		}
			[JsonIgnore] 
						private String _link;			
				
		[JsonProperty ("link", NullValueHandling = NullValueHandling.Ignore)]
	public String link
		{
			get { return _link ; }//não primitivo
			set { SetProperty (ref  _link , value); }
		}
			[JsonIgnore] 
						private String _descricao;			
				
		[JsonProperty ("descricao", NullValueHandling = NullValueHandling.Ignore)]
	public String descricao
		{
			get { return _descricao ; }//não primitivo
			set { SetProperty (ref  _descricao , value); }
		}
			[JsonIgnore] 
						private String _status;			
				
		[JsonProperty ("status", NullValueHandling = NullValueHandling.Ignore)]
	public String status
		{
			get { return _status ; }//não primitivo
			set { SetProperty (ref  _status , value); }
		}
			[JsonIgnore] 
						private String _imagemTop;			
				
		[JsonProperty ("imagemTop", NullValueHandling = NullValueHandling.Ignore)]
	public String imagemTop
		{
			get { return _imagemTop ; }//não primitivo
			set { SetProperty (ref  _imagemTop , value); }
		}
			[JsonIgnore] 
						private String _imagemFeed;			
				
		[JsonProperty ("imagemFeed", NullValueHandling = NullValueHandling.Ignore)]
	public String imagemFeed
		{
			get { return _imagemFeed ; }//não primitivo
			set { SetProperty (ref  _imagemFeed , value); }
		}
			[JsonIgnore] 
						private DateTime _dtPublicacao;			
				
		[JsonProperty ("dtPublicacao", NullValueHandling = NullValueHandling.Ignore)]
	public DateTime dtPublicacao
		{
			get { return _dtPublicacao; } //primitivo
		set { SetProperty (ref  _dtPublicacao , value); }
		}
			[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			[JsonIgnore] 
								
	private ObservableCollection<String> _aplicativoId = new ObservableCollection<String>();			
		[JsonProperty ("aplicativoId", NullValueHandling = NullValueHandling.Ignore)]
		public ObservableCollection<String> aplicativoId
		{
			get { return _aplicativoId; }
			set { _aplicativoId = value; }
		}
				
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class Aplicativo : LBModel
	{
		[JsonIgnore] 
						private string _codigoApp;			
				
		[JsonProperty ("codigoApp", NullValueHandling = NullValueHandling.Ignore)]
	public string codigoApp
		{
			get { return _codigoApp ; }//não primitivo
			set { SetProperty (ref  _codigoApp , value); }
		}
			[JsonIgnore] 
						private String _nome;			
				
		[JsonProperty ("nome", NullValueHandling = NullValueHandling.Ignore)]
	public String nome
		{
			get { return _nome ; }//não primitivo
			set { SetProperty (ref  _nome , value); }
		}
			[JsonIgnore] 
						private String _descricao;			
				
		[JsonProperty ("descricao", NullValueHandling = NullValueHandling.Ignore)]
	public String descricao
		{
			get { return _descricao ; }//não primitivo
			set { SetProperty (ref  _descricao , value); }
		}
			[JsonIgnore] 
						private bool _inativo;			
				
		[JsonProperty ("inativo", NullValueHandling = NullValueHandling.Ignore)]
	public bool inativo
		{
			get { return _inativo; } //primitivo
		set { SetProperty (ref  _inativo , value); }
		}
			[JsonIgnore] 
						private String _pushAppId;			
				
		[JsonProperty ("pushAppId", NullValueHandling = NullValueHandling.Ignore)]
	public String pushAppId
		{
			get { return _pushAppId ; }//não primitivo
			set { SetProperty (ref  _pushAppId , value); }
		}
			[JsonIgnore] 
						private String _pushKeyAuth;			
				
		[JsonProperty ("pushKeyAuth", NullValueHandling = NullValueHandling.Ignore)]
	public String pushKeyAuth
		{
			get { return _pushKeyAuth ; }//não primitivo
			set { SetProperty (ref  _pushKeyAuth , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return codigoApp;
		}
	}
	public partial class ExamePreparo : LBModel
	{
		[JsonIgnore] 
						private String _nome;			
				
		[JsonProperty ("nome", NullValueHandling = NullValueHandling.Ignore)]
	public String nome
		{
			get { return _nome ; }//não primitivo
			set { SetProperty (ref  _nome , value); }
		}
			[JsonIgnore] 
						private String _descricao;			
				
		[JsonProperty ("descricao", NullValueHandling = NullValueHandling.Ignore)]
	public String descricao
		{
			get { return _descricao ; }//não primitivo
			set { SetProperty (ref  _descricao , value); }
		}
			[JsonIgnore] 
						private bool _excluido;			
				
		[JsonProperty ("excluido", NullValueHandling = NullValueHandling.Ignore)]
	public bool excluido
		{
			get { return _excluido; } //primitivo
		set { SetProperty (ref  _excluido , value); }
		}
			[JsonIgnore] 
								
	private ObservableCollection<ExameDetalhe> _detalhe = new ObservableCollection<ExameDetalhe>();			
		[JsonProperty ("detalhe", NullValueHandling = NullValueHandling.Ignore)]
		public ObservableCollection<ExameDetalhe> detalhe
		{
			get { return _detalhe; }
			set { _detalhe = value; }
		}
				[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			[JsonIgnore] 
								
	private ObservableCollection<String> _aplicativoId = new ObservableCollection<String>();			
		[JsonProperty ("aplicativoId", NullValueHandling = NullValueHandling.Ignore)]
		public ObservableCollection<String> aplicativoId
		{
			get { return _aplicativoId; }
			set { _aplicativoId = value; }
		}
				
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class Agendamento : LBModel
	{
		[JsonIgnore] 
						private DateTime _dataCriacao;			
				
		[JsonProperty ("dataCriacao", NullValueHandling = NullValueHandling.Ignore)]
	public DateTime dataCriacao
		{
			get { return _dataCriacao; } //primitivo
		set { SetProperty (ref  _dataCriacao , value); }
		}
			[JsonIgnore] 
						private DateTime _dataDesejada;			
				
		[JsonProperty ("dataDesejada", NullValueHandling = NullValueHandling.Ignore)]
	public DateTime dataDesejada
		{
			get { return _dataDesejada; } //primitivo
		set { SetProperty (ref  _dataDesejada , value); }
		}
			[JsonIgnore] 
						private String _periodoDesejado;			
				
		[JsonProperty ("periodoDesejado", NullValueHandling = NullValueHandling.Ignore)]
	public String periodoDesejado
		{
			get { return _periodoDesejado ; }//não primitivo
			set { SetProperty (ref  _periodoDesejado , value); }
		}
			[JsonIgnore] 
						private String _status;			
				
		[JsonProperty ("status", NullValueHandling = NullValueHandling.Ignore)]
	public String status
		{
			get { return _status ; }//não primitivo
			set { SetProperty (ref  _status , value); }
		}
			[JsonIgnore] 
						private DateTime _dataAgendamento;			
				
		[JsonProperty ("dataAgendamento", NullValueHandling = NullValueHandling.Ignore)]
	public DateTime dataAgendamento
		{
			get { return _dataAgendamento; } //primitivo
		set { SetProperty (ref  _dataAgendamento , value); }
		}
			[JsonIgnore] 
						private DateTime _horaAgendamento;			
				
		[JsonProperty ("horaAgendamento", NullValueHandling = NullValueHandling.Ignore)]
	public DateTime horaAgendamento
		{
			get { return _horaAgendamento; } //primitivo
		set { SetProperty (ref  _horaAgendamento , value); }
		}
			[JsonIgnore] 
						private String _localAgendamento;			
				
		[JsonProperty ("localAgendamento", NullValueHandling = NullValueHandling.Ignore)]
	public String localAgendamento
		{
			get { return _localAgendamento ; }//não primitivo
			set { SetProperty (ref  _localAgendamento , value); }
		}
			[JsonIgnore] 
						private String _obs;			
				
		[JsonProperty ("obs", NullValueHandling = NullValueHandling.Ignore)]
	public String obs
		{
			get { return _obs ; }//não primitivo
			set { SetProperty (ref  _obs , value); }
		}
			[JsonIgnore] 
						private String _imgPreAgendamento;			
				
		[JsonProperty ("imgPreAgendamento", NullValueHandling = NullValueHandling.Ignore)]
	public String imgPreAgendamento
		{
			get { return _imgPreAgendamento ; }//não primitivo
			set { SetProperty (ref  _imgPreAgendamento , value); }
		}
			[JsonIgnore] 
						private bool _excluido;			
				
		[JsonProperty ("excluido", NullValueHandling = NullValueHandling.Ignore)]
	public bool excluido
		{
			get { return _excluido; } //primitivo
		set { SetProperty (ref  _excluido , value); }
		}
			[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			[JsonIgnore] 
						private String _clienteId;			
				
		[JsonProperty ("clienteId", NullValueHandling = NullValueHandling.Ignore)]
	public String clienteId
		{
			get { return _clienteId ; }//não primitivo
			set { SetProperty (ref  _clienteId , value); }
		}
			[JsonIgnore] 
						private String _atendenteId;			
				
		[JsonProperty ("atendenteId", NullValueHandling = NullValueHandling.Ignore)]
	public String atendenteId
		{
			get { return _atendenteId ; }//não primitivo
			set { SetProperty (ref  _atendenteId , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class Instalacao : LBModel
	{
		[JsonIgnore] 
						private String _deviceToken;			
				
		[JsonProperty ("deviceToken", NullValueHandling = NullValueHandling.Ignore)]
	public String deviceToken
		{
			get { return _deviceToken ; }//não primitivo
			set { SetProperty (ref  _deviceToken , value); }
		}
			[JsonIgnore] 
						private String _deviceType;			
				
		[JsonProperty ("deviceType", NullValueHandling = NullValueHandling.Ignore)]
	public String deviceType
		{
			get { return _deviceType ; }//não primitivo
			set { SetProperty (ref  _deviceType , value); }
		}
			[JsonIgnore] 
						private DateTime _dataCriacao;			
				
		[JsonProperty ("dataCriacao", NullValueHandling = NullValueHandling.Ignore)]
	public DateTime dataCriacao
		{
			get { return _dataCriacao; } //primitivo
		set { SetProperty (ref  _dataCriacao , value); }
		}
			[JsonIgnore] 
						private bool _inativo;			
				
		[JsonProperty ("inativo", NullValueHandling = NullValueHandling.Ignore)]
	public bool inativo
		{
			get { return _inativo; } //primitivo
		set { SetProperty (ref  _inativo , value); }
		}
			[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			[JsonIgnore] 
						private String _pessoaId;			
				
		[JsonProperty ("pessoaId", NullValueHandling = NullValueHandling.Ignore)]
	public String pessoaId
		{
			get { return _pessoaId ; }//não primitivo
			set { SetProperty (ref  _pessoaId , value); }
		}
			[JsonIgnore] 
						private String _aplicativoId;			
				
		[JsonProperty ("aplicativoId", NullValueHandling = NullValueHandling.Ignore)]
	public String aplicativoId
		{
			get { return _aplicativoId ; }//não primitivo
			set { SetProperty (ref  _aplicativoId , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class Notificacao : LBModel
	{
		[JsonIgnore] 
						private String _titulo;			
				
		[JsonProperty ("titulo", NullValueHandling = NullValueHandling.Ignore)]
	public String titulo
		{
			get { return _titulo ; }//não primitivo
			set { SetProperty (ref  _titulo , value); }
		}
			[JsonIgnore] 
						private String _mensagem;			
				
		[JsonProperty ("mensagem", NullValueHandling = NullValueHandling.Ignore)]
	public String mensagem
		{
			get { return _mensagem ; }//não primitivo
			set { SetProperty (ref  _mensagem , value); }
		}
			[JsonIgnore] 
						private String _tipo;			
				
		[JsonProperty ("tipo", NullValueHandling = NullValueHandling.Ignore)]
	public String tipo
		{
			get { return _tipo ; }//não primitivo
			set { SetProperty (ref  _tipo , value); }
		}
			[JsonIgnore] 
						private DateTime _dataCriacao;			
				
		[JsonProperty ("dataCriacao", NullValueHandling = NullValueHandling.Ignore)]
	public DateTime dataCriacao
		{
			get { return _dataCriacao; } //primitivo
		set { SetProperty (ref  _dataCriacao , value); }
		}
			[JsonIgnore] 
						private String _status;			
				
		[JsonProperty ("status", NullValueHandling = NullValueHandling.Ignore)]
	public String status
		{
			get { return _status ; }//não primitivo
			set { SetProperty (ref  _status , value); }
		}
			[JsonIgnore] 
						private DateTime _dataEnvio;			
				
		[JsonProperty ("dataEnvio", NullValueHandling = NullValueHandling.Ignore)]
	public DateTime dataEnvio
		{
			get { return _dataEnvio; } //primitivo
		set { SetProperty (ref  _dataEnvio , value); }
		}
			[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			[JsonIgnore] 
								
	private ObservableCollection<String> _pessoaId = new ObservableCollection<String>();			
		[JsonProperty ("pessoaId", NullValueHandling = NullValueHandling.Ignore)]
		public ObservableCollection<String> pessoaId
		{
			get { return _pessoaId; }
			set { _pessoaId = value; }
		}
				[JsonIgnore] 
						private String _aplicativoId;			
				
		[JsonProperty ("aplicativoId", NullValueHandling = NullValueHandling.Ignore)]
	public String aplicativoId
		{
			get { return _aplicativoId ; }//não primitivo
			set { SetProperty (ref  _aplicativoId , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class Dispositivo : LBModel
	{
		[JsonIgnore] 
						private String _uniqueId;			
				
		[JsonProperty ("uniqueId", NullValueHandling = NullValueHandling.Ignore)]
	public String uniqueId
		{
			get { return _uniqueId ; }//não primitivo
			set { SetProperty (ref  _uniqueId , value); }
		}
			[JsonIgnore] 
						private String _apiVersion;			
				
		[JsonProperty ("apiVersion", NullValueHandling = NullValueHandling.Ignore)]
	public String apiVersion
		{
			get { return _apiVersion ; }//não primitivo
			set { SetProperty (ref  _apiVersion , value); }
		}
			[JsonIgnore] 
						private String _system;			
				
		[JsonProperty ("system", NullValueHandling = NullValueHandling.Ignore)]
	public String system
		{
			get { return _system ; }//não primitivo
			set { SetProperty (ref  _system , value); }
		}
			[JsonIgnore] 
						private String _architecture;			
				
		[JsonProperty ("architecture", NullValueHandling = NullValueHandling.Ignore)]
	public String architecture
		{
			get { return _architecture ; }//não primitivo
			set { SetProperty (ref  _architecture , value); }
		}
			[JsonIgnore] 
						private String _name;			
				
		[JsonProperty ("name", NullValueHandling = NullValueHandling.Ignore)]
	public String name
		{
			get { return _name ; }//não primitivo
			set { SetProperty (ref  _name , value); }
		}
			[JsonIgnore] 
						private String _manufacturer;			
				
		[JsonProperty ("manufacturer", NullValueHandling = NullValueHandling.Ignore)]
	public String manufacturer
		{
			get { return _manufacturer ; }//não primitivo
			set { SetProperty (ref  _manufacturer , value); }
		}
			[JsonIgnore] 
						private String _model;			
				
		[JsonProperty ("model", NullValueHandling = NullValueHandling.Ignore)]
	public String model
		{
			get { return _model ; }//não primitivo
			set { SetProperty (ref  _model , value); }
		}
			[JsonIgnore] 
						private String _networkCarrier;			
				
		[JsonProperty ("networkCarrier", NullValueHandling = NullValueHandling.Ignore)]
	public String networkCarrier
		{
			get { return _networkCarrier ; }//não primitivo
			set { SetProperty (ref  _networkCarrier , value); }
		}
			[JsonIgnore] 
						private String _platform;			
				
		[JsonProperty ("platform", NullValueHandling = NullValueHandling.Ignore)]
	public String platform
		{
			get { return _platform ; }//não primitivo
			set { SetProperty (ref  _platform , value); }
		}
			[JsonIgnore] 
						private String _idiom;			
				
		[JsonProperty ("idiom", NullValueHandling = NullValueHandling.Ignore)]
	public String idiom
		{
			get { return _idiom ; }//não primitivo
			set { SetProperty (ref  _idiom , value); }
		}
			[JsonIgnore] 
						private String _versionNumber;			
				
		[JsonProperty ("versionNumber", NullValueHandling = NullValueHandling.Ignore)]
	public String versionNumber
		{
			get { return _versionNumber ; }//não primitivo
			set { SetProperty (ref  _versionNumber , value); }
		}
			[JsonIgnore] 
						private DateTime _dataCriacao;			
				
		[JsonProperty ("dataCriacao", NullValueHandling = NullValueHandling.Ignore)]
	public DateTime dataCriacao
		{
			get { return _dataCriacao; } //primitivo
		set { SetProperty (ref  _dataCriacao , value); }
		}
			[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			[JsonIgnore] 
						private String _pessoaId;			
				
		[JsonProperty ("pessoaId", NullValueHandling = NullValueHandling.Ignore)]
	public String pessoaId
		{
			get { return _pessoaId ; }//não primitivo
			set { SetProperty (ref  _pessoaId , value); }
		}
			[JsonIgnore] 
						private String _aplicativoId;			
				
		[JsonProperty ("aplicativoId", NullValueHandling = NullValueHandling.Ignore)]
	public String aplicativoId
		{
			get { return _aplicativoId ; }//não primitivo
			set { SetProperty (ref  _aplicativoId , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class PessoaRegistro : LBModel
	{
		[JsonIgnore] 
						private String _pessoaId;			
				
		[JsonProperty ("pessoaId", NullValueHandling = NullValueHandling.Ignore)]
	public String pessoaId
		{
			get { return _pessoaId ; }//não primitivo
			set { SetProperty (ref  _pessoaId , value); }
		}
			[JsonIgnore] 
						private DateTime _dtCadastro;			
				
		[JsonProperty ("dtCadastro", NullValueHandling = NullValueHandling.Ignore)]
	public DateTime dtCadastro
		{
			get { return _dtCadastro; } //primitivo
		set { SetProperty (ref  _dtCadastro , value); }
		}
			[JsonIgnore] 
						private double _modelo;			
				
		[JsonProperty ("modelo", NullValueHandling = NullValueHandling.Ignore)]
	public double modelo
		{
			get { return _modelo; } //primitivo
		set { SetProperty (ref  _modelo , value); }
		}
			[JsonIgnore] 
						private double _tipo;			
				
		[JsonProperty ("tipo", NullValueHandling = NullValueHandling.Ignore)]
	public double tipo
		{
			get { return _tipo; } //primitivo
		set { SetProperty (ref  _tipo , value); }
		}
			[JsonIgnore] 
						private String _usuarioId;			
				
		[JsonProperty ("usuarioId", NullValueHandling = NullValueHandling.Ignore)]
	public String usuarioId
		{
			get { return _usuarioId ; }//não primitivo
			set { SetProperty (ref  _usuarioId , value); }
		}
			[JsonIgnore] 
						private bool _excluido;			
				
		[JsonProperty ("excluido", NullValueHandling = NullValueHandling.Ignore)]
	public bool excluido
		{
			get { return _excluido; } //primitivo
		set { SetProperty (ref  _excluido , value); }
		}
			[JsonIgnore] 
						private String _excluidoPor;			
				
		[JsonProperty ("excluidoPor", NullValueHandling = NullValueHandling.Ignore)]
	public String excluidoPor
		{
			get { return _excluidoPor ; }//não primitivo
			set { SetProperty (ref  _excluidoPor , value); }
		}
			[JsonIgnore] 
						private DateTime _dtExclusao;			
				
		[JsonProperty ("dtExclusao", NullValueHandling = NullValueHandling.Ignore)]
	public DateTime dtExclusao
		{
			get { return _dtExclusao; } //primitivo
		set { SetProperty (ref  _dtExclusao , value); }
		}
			[JsonIgnore] 
						private String _motivoExclusao;			
				
		[JsonProperty ("motivoExclusao", NullValueHandling = NullValueHandling.Ignore)]
	public String motivoExclusao
		{
			get { return _motivoExclusao ; }//não primitivo
			set { SetProperty (ref  _motivoExclusao , value); }
		}
			[JsonIgnore] 
						private String _imagem;			
				
		[JsonProperty ("imagem", NullValueHandling = NullValueHandling.Ignore)]
	public String imagem
		{
			get { return _imagem ; }//não primitivo
			set { SetProperty (ref  _imagem , value); }
		}
			[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			[JsonIgnore] 
						private SinalVital _registroSinalVital;			
				
		[JsonProperty ("registroSinalVital", NullValueHandling = NullValueHandling.Ignore)]
	public SinalVital registroSinalVital
		{
			get { return _registroSinalVital ; }//não primitivo
			set { SetProperty (ref  _registroSinalVital , value); }
		}
			[JsonIgnore] 
						private PessoaMedicamento _registroMedicamento;			
				
		[JsonProperty ("registroMedicamento", NullValueHandling = NullValueHandling.Ignore)]
	public PessoaMedicamento registroMedicamento
		{
			get { return _registroMedicamento ; }//não primitivo
			set { SetProperty (ref  _registroMedicamento , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class PessoaAlergia : LBModel
	{
		[JsonIgnore] 
								
	private ObservableCollection<Object> _alimentar = new ObservableCollection<Object>();			
		[JsonProperty ("alimentar", NullValueHandling = NullValueHandling.Ignore)]
		public ObservableCollection<Object> alimentar
		{
			get { return _alimentar; }
			set { _alimentar = value; }
		}
				[JsonIgnore] 
								
	private ObservableCollection<Object> _medicamentos = new ObservableCollection<Object>();			
		[JsonProperty ("medicamentos", NullValueHandling = NullValueHandling.Ignore)]
		public ObservableCollection<Object> medicamentos
		{
			get { return _medicamentos; }
			set { _medicamentos = value; }
		}
				[JsonIgnore] 
						private String _outras;			
				
		[JsonProperty ("outras", NullValueHandling = NullValueHandling.Ignore)]
	public String outras
		{
			get { return _outras ; }//não primitivo
			set { SetProperty (ref  _outras , value); }
		}
			[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			[JsonIgnore] 
						private String _pessoaId;			
				
		[JsonProperty ("pessoaId", NullValueHandling = NullValueHandling.Ignore)]
	public String pessoaId
		{
			get { return _pessoaId ; }//não primitivo
			set { SetProperty (ref  _pessoaId , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class PessoaHistoricoFamiliar : LBModel
	{
		[JsonIgnore] 
								
	private ObservableCollection<Object> _geracaoAtual = new ObservableCollection<Object>();			
		[JsonProperty ("geracaoAtual", NullValueHandling = NullValueHandling.Ignore)]
		public ObservableCollection<Object> geracaoAtual
		{
			get { return _geracaoAtual; }
			set { _geracaoAtual = value; }
		}
				[JsonIgnore] 
								
	private ObservableCollection<Object> _geracaoPais = new ObservableCollection<Object>();			
		[JsonProperty ("geracaoPais", NullValueHandling = NullValueHandling.Ignore)]
		public ObservableCollection<Object> geracaoPais
		{
			get { return _geracaoPais; }
			set { _geracaoPais = value; }
		}
				[JsonIgnore] 
								
	private ObservableCollection<Object> _geracaoAvos = new ObservableCollection<Object>();			
		[JsonProperty ("geracaoAvos", NullValueHandling = NullValueHandling.Ignore)]
		public ObservableCollection<Object> geracaoAvos
		{
			get { return _geracaoAvos; }
			set { _geracaoAvos = value; }
		}
				[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			[JsonIgnore] 
						private String _pessoaId;			
				
		[JsonProperty ("pessoaId", NullValueHandling = NullValueHandling.Ignore)]
	public String pessoaId
		{
			get { return _pessoaId ; }//não primitivo
			set { SetProperty (ref  _pessoaId , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class PessoaProtocolo : LBModel
	{
		[JsonIgnore] 
						private String _nrProtocolo;			
				
		[JsonProperty ("nrProtocolo", NullValueHandling = NullValueHandling.Ignore)]
	public String nrProtocolo
		{
			get { return _nrProtocolo ; }//não primitivo
			set { SetProperty (ref  _nrProtocolo , value); }
		}
			[JsonIgnore] 
						private String _senhaProtocolo;			
				
		[JsonProperty ("senhaProtocolo", NullValueHandling = NullValueHandling.Ignore)]
	public String senhaProtocolo
		{
			get { return _senhaProtocolo ; }//não primitivo
			set { SetProperty (ref  _senhaProtocolo , value); }
		}
			[JsonIgnore] 
						private DateTime _dtCadastro;			
				
		[JsonProperty ("dtCadastro", NullValueHandling = NullValueHandling.Ignore)]
	public DateTime dtCadastro
		{
			get { return _dtCadastro; } //primitivo
		set { SetProperty (ref  _dtCadastro , value); }
		}
			[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			[JsonIgnore] 
						private String _pessoaId;			
				
		[JsonProperty ("pessoaId", NullValueHandling = NullValueHandling.Ignore)]
	public String pessoaId
		{
			get { return _pessoaId ; }//não primitivo
			set { SetProperty (ref  _pessoaId , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class Protocolo : LBModel
	{
		[JsonIgnore] 
						private String _nrProtocolo;			
				
		[JsonProperty ("nrProtocolo", NullValueHandling = NullValueHandling.Ignore)]
	public String nrProtocolo
		{
			get { return _nrProtocolo ; }//não primitivo
			set { SetProperty (ref  _nrProtocolo , value); }
		}
			[JsonIgnore] 
						private String _senhaProtocolo;			
				
		[JsonProperty ("senhaProtocolo", NullValueHandling = NullValueHandling.Ignore)]
	public String senhaProtocolo
		{
			get { return _senhaProtocolo ; }//não primitivo
			set { SetProperty (ref  _senhaProtocolo , value); }
		}
			[JsonIgnore] 
						private DateTime _dtCadastro;			
				
		[JsonProperty ("dtCadastro", NullValueHandling = NullValueHandling.Ignore)]
	public DateTime dtCadastro
		{
			get { return _dtCadastro; } //primitivo
		set { SetProperty (ref  _dtCadastro , value); }
		}
			[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			[JsonIgnore] 
						private String _pessoaId;			
				
		[JsonProperty ("pessoaId", NullValueHandling = NullValueHandling.Ignore)]
	public String pessoaId
		{
			get { return _pessoaId ; }//não primitivo
			set { SetProperty (ref  _pessoaId , value); }
		}
			[JsonIgnore] 
								
	private ObservableCollection<ProtocoloDetalhe> _detalhe = new ObservableCollection<ProtocoloDetalhe>();			
		[JsonProperty ("detalhe", NullValueHandling = NullValueHandling.Ignore)]
		public ObservableCollection<ProtocoloDetalhe> detalhe
		{
			get { return _detalhe; }
			set { _detalhe = value; }
		}
				
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class ProtocoloDetalhe : LBModel
	{
		[JsonIgnore] 
						private String _nomeExame;			
				
		[JsonProperty ("nomeExame", NullValueHandling = NullValueHandling.Ignore)]
	public String nomeExame
		{
			get { return _nomeExame ; }//não primitivo
			set { SetProperty (ref  _nomeExame , value); }
		}
			[JsonIgnore] 
						private String _linkDocumento;			
				
		[JsonProperty ("linkDocumento", NullValueHandling = NullValueHandling.Ignore)]
	public String linkDocumento
		{
			get { return _linkDocumento ; }//não primitivo
			set { SetProperty (ref  _linkDocumento , value); }
		}
			[JsonIgnore] 
						private DateTime _dtExame;			
				
		[JsonProperty ("dtExame", NullValueHandling = NullValueHandling.Ignore)]
	public DateTime dtExame
		{
			get { return _dtExame; } //primitivo
		set { SetProperty (ref  _dtExame , value); }
		}
			[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class SinalVital : LBModel
	{
		[JsonIgnore] 
						private DateTime _data;			
				
		[JsonProperty ("data", NullValueHandling = NullValueHandling.Ignore)]
	public DateTime data
		{
			get { return _data; } //primitivo
		set { SetProperty (ref  _data , value); }
		}
			[JsonIgnore] 
						private double _pressaoMaxima;			
				
		[JsonProperty ("pressaoMaxima", NullValueHandling = NullValueHandling.Ignore)]
	public double pressaoMaxima
		{
			get { return _pressaoMaxima; } //primitivo
		set { SetProperty (ref  _pressaoMaxima , value); }
		}
			[JsonIgnore] 
						private double _pressaoMinima;			
				
		[JsonProperty ("pressaoMinima", NullValueHandling = NullValueHandling.Ignore)]
	public double pressaoMinima
		{
			get { return _pressaoMinima; } //primitivo
		set { SetProperty (ref  _pressaoMinima , value); }
		}
			[JsonIgnore] 
						private double _frequenciaRespiratoria;			
				
		[JsonProperty ("frequenciaRespiratoria", NullValueHandling = NullValueHandling.Ignore)]
	public double frequenciaRespiratoria
		{
			get { return _frequenciaRespiratoria; } //primitivo
		set { SetProperty (ref  _frequenciaRespiratoria , value); }
		}
			[JsonIgnore] 
						private double _frequenciaCardiaca;			
				
		[JsonProperty ("frequenciaCardiaca", NullValueHandling = NullValueHandling.Ignore)]
	public double frequenciaCardiaca
		{
			get { return _frequenciaCardiaca; } //primitivo
		set { SetProperty (ref  _frequenciaCardiaca , value); }
		}
			[JsonIgnore] 
						private double _temperatura;			
				
		[JsonProperty ("temperatura", NullValueHandling = NullValueHandling.Ignore)]
	public double temperatura
		{
			get { return _temperatura; } //primitivo
		set { SetProperty (ref  _temperatura , value); }
		}
			[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class PessoaMedicamento : LBModel
	{
		[JsonIgnore] 
						private String _nome;			
				
		[JsonProperty ("nome", NullValueHandling = NullValueHandling.Ignore)]
	public String nome
		{
			get { return _nome ; }//não primitivo
			set { SetProperty (ref  _nome , value); }
		}
			[JsonIgnore] 
						private String _descricao;			
				
		[JsonProperty ("descricao", NullValueHandling = NullValueHandling.Ignore)]
	public String descricao
		{
			get { return _descricao ; }//não primitivo
			set { SetProperty (ref  _descricao , value); }
		}
			[JsonIgnore] 
						private String _imgMedicamento;			
				
		[JsonProperty ("imgMedicamento", NullValueHandling = NullValueHandling.Ignore)]
	public String imgMedicamento
		{
			get { return _imgMedicamento ; }//não primitivo
			set { SetProperty (ref  _imgMedicamento , value); }
		}
			[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class CadastroPessoa : LBModel
	{
		[JsonIgnore] 
						private Pessoa _pessoa;			
				
		[JsonProperty ("pessoa", NullValueHandling = NullValueHandling.Ignore)]
	public Pessoa pessoa
		{
			get { return _pessoa ; }//não primitivo
			set { SetProperty (ref  _pessoa , value); }
		}
			[JsonIgnore] 
						private User _user;			
				
		[JsonProperty ("user", NullValueHandling = NullValueHandling.Ignore)]
	public User user
		{
			get { return _user ; }//não primitivo
			set { SetProperty (ref  _user , value); }
		}
			[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class ExameDetalhe : LBModel
	{
		[JsonIgnore] 
						private string _id;			
				
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
	public string id
		{
			get { return _id ; }//não primitivo
			set { SetProperty (ref  _id , value); }
		}
			[JsonIgnore] 
						private String _grupo;			
				
		[JsonProperty ("grupo", NullValueHandling = NullValueHandling.Ignore)]
	public String grupo
		{
			get { return _grupo ; }//não primitivo
			set { SetProperty (ref  _grupo , value); }
		}
			[JsonIgnore] 
						private String _conteudo;			
				
		[JsonProperty ("conteudo", NullValueHandling = NullValueHandling.Ignore)]
	public String conteudo
		{
			get { return _conteudo ; }//não primitivo
			set { SetProperty (ref  _conteudo , value); }
		}
			[JsonIgnore] 
						private String _icone;			
				
		[JsonProperty ("icone", NullValueHandling = NullValueHandling.Ignore)]
	public String icone
		{
			get { return _icone ; }//não primitivo
			set { SetProperty (ref  _icone , value); }
		}
			
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}

	// Relationship classes:
	// None.
}
// Eof
