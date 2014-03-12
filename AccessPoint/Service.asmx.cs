﻿using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using Crawler;
using Microsoft.WindowsAzure.Storage.Table.DataServices;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using System.Configuration;

namespace AccessPoint
{
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class Service : System.Web.Services.WebService
    {
        private static CloudTable table;
        private static CloudTable dataTable;
        private static CloudQueue commandQueue;
        private static CloudQueue urlQueue;
        private static bool created = false;
        private static Dictionary<string, List<string>> cache;

        public Service()
        {
            if (created == false)
            {
                string connectionString = connectionString = ConfigurationManager.ConnectionStrings["StorageConnectionString"].ConnectionString;
                CloudStorageAccount storage = CloudStorageAccount.Parse(connectionString);
                CloudQueueClient queueClient = storage.CreateCloudQueueClient();
                commandQueue = queueClient.GetQueueReference("commandqueue");
                commandQueue.CreateIfNotExists();
                urlQueue = queueClient.GetQueueReference("urlqueue");
                urlQueue.CreateIfNotExists();

                CloudTableClient tableClient = storage.CreateCloudTableClient();
                table = tableClient.GetTableReference("urltable");
                table.CreateIfNotExists();
                dataTable = tableClient.GetTableReference("datatable");
                dataTable.CreateIfNotExists();

                cache = new Dictionary<string, List<string>>();
                created = true;
            }
        }

        private string combineFilter(List<string> words) {
            string word = words[0];
            words.RemoveAt(0);

            string next;
            if (words.Count == 0)
            {
                next = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, word);
            }
            else
            {
                next = combineFilter(words);
            }

            return TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, word),
                TableOperators.Or,
                next
            );
        }

        [WebMethod]
        public List<string> SearchUrl(string word)
        {
            List<string> list;
            if (cache.ContainsKey(word))
            {
                return cache[word];
            }
            else
            {
                TableQuery<Website> query = new TableQuery<Website>().Where(combineFilter(Regex.Split(word, "\\s").ToList()));
                list = new List<string>();
                foreach (Website site in table.ExecuteQuery(query))
                {
                    list.Add(WebUtility.UrlDecode(site.RowKey));
                }

                cache.Add(word, list);
                if (cache.Count > 100)
                {
                    cache.Clear();
                }
            }

            return list;
        }

        [WebMethod]
        public bool Command(string command)
        {
            commandQueue.AddMessage(new CloudQueueMessage(command));
            return true;
        }

        [WebMethod]
        public int SearchCacheSize()
        {
            return cache.Count;
        }

        [WebMethod]
        public int QueueSize()
        {
            string result = getData("queuesize");
            if (result != null)
            {
                return int.Parse(result);
            }
            else
            {
                return 0;
            }
        }
        
        [WebMethod]
        public int CrawledSize()
        {
            string result = getData("count");
            if (result != null)
            {
                return int.Parse(result);
            }
            else
            {
                return 0;
            }
        }

        [WebMethod]
        public int ErrorSize()
        {
            string result = getData("errorsize");
            if (result != null)
            {
                return int.Parse(result);
            }
            else
            {
                return 0;
            }
        }

        [WebMethod]
        public List<string> LastTen()
        {
            string result = getData("lastten");

            if (result != null)
            {
                return result.Split('|').ToList<string>();
            }
            else
            {
                return new List<string>();
            }
        }

        [WebMethod]
        public List<string> LastTenErrors()
        {
            string result = getData("lasttenerror");

            if (result != null)
            {
                return WebUtility.UrlDecode(result).Split('|').ToList<string>();
            }
            else
            {
                return new List<string>();
            }
        }


        [WebMethod]
        public long GetRam()
        {
            return GC.GetTotalMemory(true) / 1048576;
        }

        [WebMethod]
        public string IsRunning()
        {
            string result = getData("isrunning");
            if (result != null)
            {
                return result;
            }
            else
            {
                return "false";
            }
        }

        private string getData(string type) {
            TableQuery<Data> query = new TableQuery<Data>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "data"),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, type)
                )
            );

            List<Data> results = dataTable.ExecuteQuery(query).ToList<Data>();
            if (results.Count > 0)
            {
                return WebUtility.UrlDecode(results[0].data);
            }
            else
            {
                return null;
            }
        }
    }
}
