﻿// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming
#pragma warning disable 649

using System.Collections.Generic;

namespace DotLauncher.LibraryProviders.Origin
{
    internal class GameLocalDataResponse
    {
        public class LocalizableAttributes
        {
            public string longDescription;
            public string displayName;
        }

        public class Publishing
        {
            public class Software
            {
                public class FulfillmentAttributes
                {
                    public string executePathOverride;
                    public string installationDirectory;
                    public string installCheckOverride;
                }

                public string softwareId;
                public string softwarePlatform;
                public FulfillmentAttributes fulfillmentAttributes;
            }

            public class SoftwareList
            {
                public List<Software> software;
            }

            public SoftwareList softwareList;
        }

        public string offerId;
        public string offerType;
        public Publishing publishing;
        public LocalizableAttributes localizableAttributes;

    }
}
