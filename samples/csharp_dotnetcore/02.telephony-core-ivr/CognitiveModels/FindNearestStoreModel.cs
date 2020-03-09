using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreBot.CognitiveModels
{
    public class FindNearestStoreModel
    {
        public Storelocation[] StoreLocation { get; set; }
    }

    public class Storelocation
    {
        public string[] City { get; set; }
        public string[] State { get; set; }
    }

}
