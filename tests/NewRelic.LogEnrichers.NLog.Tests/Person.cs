// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0

namespace NewRelic.LogEnrichers.NLog.Tests
{
    public class Person
    {
        public string Name { get; set; }

        public Person Manager { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
