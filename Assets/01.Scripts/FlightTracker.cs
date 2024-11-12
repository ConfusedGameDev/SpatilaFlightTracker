using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class FlightTracker : MonoBehaviour
{
     
    public string apiKey = "ZWuYuGOivaxYcipb2QLgqyizt8oRyM7S";
    public TMPro.TMP_InputField flightNumber;

    public TextMeshProUGUI flightNumberContainer, flightOrigin, flightDestination, totalDuration, remainingTime;
    public Image flightPercentage;
    public GameObject earth;  // Assign the Earth GameObject in Unity
    public GameObject plane;  // Assign the Plane GameObject in Unity
    public float earthRadius = 10f;  // Set the radius of the Earth (or scale)
    [Range(1f,10f)]
    public float verticalOffset = 2f;  // Vertical offset from Earth's surface along the normal

    public void getFlightData()
    {
        earthRadius = earth.GetComponent<SphereCollider>().radius*verticalOffset;

        StartCoroutine(GetFlightInfo(flightNumber.text));  // Example flight number: AA100
    }
    public string data;
    public TMPro.TextMeshProUGUI infoText;
    IEnumerator GetFlightInfo(string flightNumber)
    {
        string url = $"https://aeroapi.flightaware.com/aeroapi/flights/{flightNumber}";
        infoText.text = "";
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("x-apikey", apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            infoText.text+=($"Error: {request.error}");
        }
        else
        {
            // Parse the JSON response for latitude and longitude
            
 
           
            FlightDataContainer flightData = JsonUtility.FromJson<FlightDataContainer>(request.downloadHandler.text);

            if (flightData != null && flightData.flights != null && infoText)
            {
                foreach (var flight in flightData.flights)
                {
                    if (flight.status.Contains("En Route") || flight.status.Contains("On Time"))
                    {
                        infoText.text+=($"Flight ID: {flight.ident}, Status: {flight.status}");
                         url = $"https://aeroapi.flightaware.com/aeroapi/flights/{flight.fa_flight_id}/position";

                        request = UnityWebRequest.Get(url);
                        request.SetRequestHeader("x-apikey", apiKey);

                        yield return request.SendWebRequest();

                        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                        {
                            infoText.text+=($"Error: {request.error}");
                        }
                        else
                        {
                            data = request.downloadHandler.text;
                            Flight flight2 = JsonUtility.FromJson<Flight>(request.downloadHandler.text);
                            if (flight2.last_position != null)
                            {
                                infoText.text+=($"Latitude: {flight2.last_position.latitude}, Longitude: {flight2.last_position.longitude}");
                                if (flight2 != null)
                                {

                                    infoText.text+=($"Origin Airport: {flight.origin.name} ({flight.origin.code_iata}), Departure Time: {flight.scheduled_out}");
                                    infoText.text+=($"Destination Airport: {flight.destination.name} ({flight.destination.code_iata}), Arrival Time: {flight.scheduled_in}");
                                    if (!string.IsNullOrEmpty(flight.scheduled_in))
                                    {
                                        DateTime scheduledArrival;
                                        DateTime scheduledDeparture;
                                        if (DateTime.TryParse(flight.actual_off, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out scheduledDeparture))
                                        {
                                             
                                        }
                                        if (DateTime.TryParse(flight.scheduled_in, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out scheduledArrival))
                                        {
                                            
                                        }
                                        TimeSpan duration = scheduledArrival - scheduledDeparture;
                                        TimeSpan timeUntilArrival = scheduledArrival - DateTime.Now;
                                        var percentage= timeUntilArrival.TotalSeconds/duration.TotalSeconds;
                                        flightPercentage.fillAmount =(float) percentage;
                                        totalDuration.text = "Total Duration: "+ duration.ToString();
                                        remainingTime.text = "Time until Arrival: " + timeUntilArrival.ToString();
                                        infoText.text += ($"Time Until Arrival: {timeUntilArrival}");
                                    }

                                    if (flight2.last_position != null)
                                    {
                                        infoText.text+=($"Current Position - Latitude: {flight2.last_position.latitude}, Longitude: {flight2.last_position.longitude}");
                                        if (!string.IsNullOrEmpty(flight.gate_origin))
                                        {
                                            infoText.text+=($"Gate Origin: {flight2.gate_origin}, Terminal Origin: {flight2.terminal_origin}");
                                        }
                                        if (!string.IsNullOrEmpty(flight2.gate_destination))
                                        {
                                            infoText.text+=($"Gate Destination: {flight2.gate_destination}, Terminal Destination: {flight2.terminal_destination}");
                                        }

                                        // Calculate the position on Earth using latitude and longitude
                                        //string earthPosition = GetEarthPosition(flight.last_position.latitude, flight.last_position.longitude);
                                        infoText.text+=($"Position on Earth:   Lat: {flight2.last_position.latitude}  Long: {flight2.last_position.longitude}");
                                    }
                                    //SetPlanePositionOnEarth(flight2.last_position.latitude, flight2.last_position.longitude);
                                }
                                if(flightNumberContainer && flightOrigin && flightDestination)
                                {
                                    flightNumberContainer.text = "Flight # " + flightNumber;
                                    flightOrigin.text = $"{flight2.origin.name}, {flight2.origin.city}";
                                    flightDestination.text= $"{flight2.destination.name}, {flight2.destination.city}";
                                }
                            }
                             

                            
                        }
                    }
                }
            }
            // Log for debugging
            

            // Set the plane's position on Earth
            //SetPlanePositionOnEarth(latitude, longitude);
        }
    }
   

    void SetPlanePositionOnEarth(float latitude, float longitude)
    {
        // Convert latitude/longitude to a position on the Earth (3D sphere)
        Vector3 positionOnEarth = LatLonToSpherePosition(latitude, longitude, earthRadius);

        // Get the normal (direction from Earth's center to the position)
        Vector3 earthCenter = earth.transform.position;
        Vector3 directionFromCenter = (positionOnEarth - earthCenter).normalized;

        // Apply the vertical offset along the normal (add the offset to the direction)
        Vector3 finalPosition = positionOnEarth + directionFromCenter ;

        // Set the Plane's position
        plane.transform.position = finalPosition;

        // Optionally, rotate the plane to match the Earth's surface or heading direction
        plane.transform.up = directionFromCenter;
    }

    Vector3 LatLonToSpherePosition(float latitude, float longitude, float radius)
    {
        // Convert latitude and longitude from degrees to radians
        float lat = Mathf.Deg2Rad * latitude;
        float lon = Mathf.Deg2Rad * longitude;

        // Calculate x, y, z coordinates on the sphere
        float x = radius * Mathf.Cos(lat) * Mathf.Cos(lon);
        float y = radius * Mathf.Sin(lat);
        float z = radius * Mathf.Cos(lat) * Mathf.Sin(lon);

        return new Vector3(x, y, z);
    }
}
[System.Serializable]
public class FlightPosition
{
    public float latitude;
    public float longitude;
}
[Serializable]
public class FlightDataContainer
{
    public List<Flight> flights;
    public object links;
    public int num_pages;
}

[Serializable]
public class Flight
{
    public string ident;
    public string ident_icao;
    public string ident_iata;
    public string fa_flight_id;
    public string actual_runway_off;
    public string actual_runway_on;
    public string actual_off;
    public string actual_on;
    public bool foresight_predictions_available;
    public string predicted_out;
    public string predicted_off;
    public string predicted_on;
    public string predicted_in;
    public string predicted_out_source;
    public string predicted_off_source;
    public string predicted_on_source;
    public string predicted_in_source;
    public Airport origin;
    public Airport destination;
    public List<string> codeshares;
    public List<string> codeshares_iata;
    public int departure_delay;
    public int arrival_delay;
    public int filed_ete;
    public string scheduled_out;
    public string estimated_out;
    public string scheduled_off;
    public string estimated_off;
    public string scheduled_on;
    public string estimated_on;
    public string scheduled_in;
    public string estimated_in;
    public int progress_percent;
    public string status;
    public string aircraft_type;
    public int route_distance;
    public int filed_airspeed;
    public int filed_altitude;
    public string route;
    public string baggage_claim;
    public int? seats_cabin_business;
    public int? seats_cabin_coach;
    public int? seats_cabin_first;
    public string gate_origin;
    public string gate_destination;
    public string terminal_origin;
    public string terminal_destination;
    public string type;
    public string atc_ident;
    public string inbound_fa_flight_id;
    public string flight_number;
    public string registration;
    public bool blocked;
    public bool diverted;
    public bool cancelled;
    public bool position_only;
    public List<float> waypoints;
    public string first_position_time;
    public LastPosition last_position;
    public List<float> bounding_box;
    public string ident_prefix;
}

[Serializable]
public class Airport
{
    public string code;
    public string code_icao;
    public string code_iata;
    public string code_lid;
    public string timezone;
    public string name;
    public string city;
    public string airport_info_url;
}

[Serializable]
public class LastPosition
{
    public string fa_flight_id;
    public int altitude;
    public string altitude_change;
    public int groundspeed;
    public int heading;
    public float latitude;
    public float longitude;
    public string timestamp;
    public string update_type;
}


