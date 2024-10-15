using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
 
using System.Threading.Tasks;
using System.IO;
using System.Collections;

namespace AeroApi4Sample
{
    public class AeroApiController : MonoBehaviour
{
    public string apiKey = "ZWuYuGOivaxYcipb2QLgqyizt8oRyM7S";
    public string flightId = "AM57";

        [ContextMenu("do stuff")]
    void Start()
    {
           

            var strApiKey = apiKey;

         
        var strIdentToLookUp = flightId;
            StartCoroutine(GetFlightsCoroutine(apiKey,flightId));
             

        

        
    }
        private IEnumerator GetFlightsCoroutine(string strApiKey, string strIdent)
        {
            Debug.Log("Trying to get info");
            Task<List<Flight>> getFlightsTask = GetFlights(strApiKey, strIdent);
            yield return new WaitUntil(() => getFlightsTask.IsCompleted);

            if (getFlightsTask.IsCompletedSuccessfully)
            {
                List<Flight> flights = getFlightsTask.Result;
                if (flights != null)
                {
                    // Handle flights result here
                    Debug.Log("Flights retrieved successfully.");
                    var nextFlightToDepart = flights.Where(
            f => f.ActualOut == null
            ).OrderBy(f => f.ScheduledOut).First();

                    Debug.Log(
                        string.Format(
                            "Next departure of {0} is {1} at {2}",
                            strIdent,
                            nextFlightToDepart.FaFlightId,
                            nextFlightToDepart.ScheduledOut
                            )
                        );
                }
            }
            else
            {
                Debug.LogError("Failed to retrieve flights.");
            }
        }
        private static async Task<List<Flight>> GetFlights(string strApiKey, string strIdent)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json")
                );
                client.DefaultRequestHeaders.Add(
                    "x-apikey",
                    strApiKey
                );
                client.Timeout = new TimeSpan(0, 0, 15);
                FlightsResult flightResult = null;
                var response = await client.GetAsync(
                    "https://aeroapi.flightaware.com/aeroapi/flights/" + strIdent
                );
                var contentStream = await response.Content.ReadAsStreamAsync();

                if (response.IsSuccessStatusCode)
                {
                    using (var reader = new StreamReader(contentStream))
                    {
                        var contentString = await reader.ReadToEndAsync();
                        flightResult = UnityEngine.JsonUtility.FromJson<FlightsResult>(contentString);
                    }
                }
                else
                {
                    Debug.LogError("API call failed: " + response);
                    return null;
                }

                return flightResult.Flights;
            }
        }
        // Start is called once before the first execution of Update after the MonoBehaviour is created

    }

    public class FlightsResult
    {
        public List<Flight> Flights { get; set; }
    }

    public class Flight
    {
        public string Ident { get; set; }

         public string FaFlightId { get; set; }

         public DateTime ScheduledOut { get; set; }

         public DateTime? ActualOut { get; set; }
    }
    
   
    }
