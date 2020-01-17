using System;
using Prometheus;
using System.Threading;
using System.Timers;
using System.Net;

namespace Prometheus_Template
{
    class Program
    {

        // these special type of varibles get written into promethues.
        private static readonly Gauge number1 =
         Metrics.CreateGauge("number_1", "Send the number 1 to the server");

        private static readonly Gauge number2 =
         Metrics.CreateGauge("number_2", "Send the number 2 to the server");

        private static readonly Gauge websiteUp =
        Metrics.CreateGauge("ping_website", "Check if website is up");

        private static readonly Gauge count100 =
        Metrics.CreateGauge("count_100", "Counts to 100");

        private static System.Timers.Timer aTimer;

        static void Main(string[] args)
        {
            Console.WriteLine("Starting our Database Writing/Tests");

            //If we wants to run our tests locally we use this code
            
            //var server = new MetricServer(hostname: "localhost", port: 1235);
            //server.Start();

            // push to this address that is running the pushgateway.exe from the Prom website.  We need the job name and the endpoint here to be the values present in the prometheus.YML file.
            var pusher = new MetricPusher(new MetricPusherOptions
            {
                Endpoint = "http://grafanaserver.westus2.cloudapp.azure.com:9091/metrics", // replace this with your domain + port 9091; The pushgateway runes on port 9091
                Job = "123" // this job name needs to equal the job name you put in the YML file to scrap localhost9091;
            });

            pusher.Start();

            // basic setting of values in prometheus.
            number1.Set(5.5);
            Console.WriteLine("Set a value to 5.5");
            setNum(number2, 10);
            Console.WriteLine("Set a value to 10");
            setNum(number2, 20);
            Console.WriteLine("Set a value to 20");


            // Ping a website every 10 seconds Example.
            checkGoogleUp();


            // Example of a More advanced pattern that should be used in conjution with Grafana. 
            //If we have a scrap time of 30 seconds it makes sense to make our results get updated every 30 seconsd, so we graph the correct latest values.
            // This also ensures that we dont skip over any values. for example if we have:
            for (int i = 0; i < 100; i++)
            {
                setNum(number2, i);
            }
            // this for loop will run a hundred times but grafana/prometheus will only print the single final value of 100. This occurs as this code will run 100 times before being scrapped and read. 
            //EG Grafana will wait 30 seconds due to user setting of a 30 second scrap time. Grafana now scraps after 30 sconds, sees the value of 100, add thats to the database and graphs it.
            // what needs to happen in this scenario is after each time setNum function completes the values need to be added to the database. 

            // To solve this problem we create ab additional thread that runs every 30 seconds independant of the main thread. This way the scrapping time and the results set time are the same.
            // This means we end up with something that effectively functions like this but doesnt look like this.

            for (int i = 0; i < 100; i++)
            {
                setNum(number2, i);    // every 30 seconds we run this value and add it to the to database. 

            }

            // is it also worth noting that you can use timers - code execution time to get an almost accurate synch between prometheus and your code. But after X amount of executions it will be slightly off.

            // To implement the above we do this this we do this : 
            countUp(null, null);

            aTimer = new System.Timers.Timer(10000);
            aTimer.Elapsed += new ElapsedEventHandler(countUp);

            // Set the Interval to 30 seconds (30000 milliseconds).
            aTimer.Interval = 30000;
            aTimer.Enabled = true;

            Console.WriteLine("Every 30 seconds we are now running a function that increases the number added to our database");

            bool runTestforever = true;

            // by not ending out test we have a monitoring solution. As every 30 seconds we are running code and adding vlaues to our database with the seperate thread we made that runs on a timer. 
            // we could also just add code inside this loop that isnt sensative to exact values and timing out with prometheus like getting the resposne code from google to see if its up
            while (runTestforever == true) 
            {
                Thread.Sleep(5000);
                checkGoogleUp();
            }

        }

        private static void setNum(Gauge promVariable, int promValue)
        {
            promVariable.Set(promValue);
        }

        private static void checkGoogleUp()
        {
            Console.WriteLine("check to see if Google is up");
            string responseCodeReturned = "";
            // if the website is online set the value to 1 if not set the value of the website variable to 0. Within grafana we can make a chart very easily that has a rule if websiteUp = 0 display text down colour=red.
            // if websiteUp = 1 display text up color = green
            try
            {

                HttpWebRequest request = WebRequest.Create("https://www.google.com") as HttpWebRequest;
                request.Timeout = 10000;
                request.Method = "HEAD"; // just get the head of an interet pack to save data.

                HttpWebResponse response = request.GetResponse() as HttpWebResponse;

                responseCodeReturned = response.StatusCode.ToString();

                if (responseCodeReturned == "OK") // OK = 200; 
                {
                    Console.WriteLine("Google is Up");
                    websiteUp.Set(1); // 

                }
                else
                {
                    Console.WriteLine("Could not return the status code of Google, status code = " + responseCodeReturned);
                    websiteUp.Set(0);//
                }
            }
            catch
            {
                Console.WriteLine("Could not return the status code of Google, status code = " + responseCodeReturned);
                websiteUp.Set(0);
            }
        }

        private static int myNum= 0;
        private static void countUp(object source, ElapsedEventArgs error)
        {
            if (myNum < 100)
            {
                myNum++;
                Console.WriteLine("Added This Num to the database " + myNum);
                count100.Set(myNum);
            }
        }
    }// end of class
}// end of namespace
