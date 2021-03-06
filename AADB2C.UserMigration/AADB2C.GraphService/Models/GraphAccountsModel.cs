﻿//===============================================================================
// Microsoft FastTrack for Azure
// Azure Active Directory B2C User Management Samples
//===============================================================================
// Copyright © Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===============================================================================
using Newtonsoft.Json;
using System.Collections.Generic;

namespace AADB2C.GraphService
{
    public class GraphAccounts
    {
        public string odatametadata { get; set; }
        public List<GraphAccountModel> value { get; set; }

        public static GraphAccounts Parse(string JSON)
        {
            return JsonConvert.DeserializeObject(JSON.Replace("odata.metadata", "odatametadata"), typeof(GraphAccounts)) as GraphAccounts;
        }
    }
}
