<a href="https://opensource.newrelic.com/oss-category/#community-plus"><picture><source media="(prefers-color-scheme: dark)" srcset="https://github.com/newrelic/opensource-website/raw/main/src/images/categories/dark/Community_Plus.png"><source media="(prefers-color-scheme: light)" srcset="https://github.com/newrelic/opensource-website/raw/main/src/images/categories/Community_Plus.png"><img alt="New Relic Open Source community plus project banner." src="https://github.com/newrelic/opensource-website/raw/main/src/images/categories/Community_Plus.png"></picture></a>

# New Relic Logging Extensions for .NET
The New Relic logging plugins are extensions for common .NET logging frameworks. They are designed to capture app, transaction trace, and span information as part of your application log messages, and format log data for forwarding to New Relic.

For the latest information, please see the [New Relic docs](https://docs.newrelic.com/docs/logs/new-relic-logs/enable-logs/enable-new-relic-logs)


## Supported Logging Frameworks

| Framework               | Version   |
|-------------------------|-----------|
| [Serilog](src/NewRelic.LogEnrichers.Serilog/README.md)             | 2.5+      |
| [NLog](src/NewRelic.LogEnrichers.NLog/README.md)                   | 4.5+      |
| [Log4Net](src/NewRelic.LogEnrichers.Log4Net/README.md) | 2.0.10+ |


## Minimum Requirements

* Microsoft <a target="_blank" href="https://dotnet.microsoft.com/download/dotnet-framework">.NET Framework 4.6.2+</a> or  <a target="_blank" href="https://dotnet.microsoft.com/download/dotnet-core">.NET 6.0+</a>
* <a target="_blank" href="https://docs.newrelic.com/docs/release-notes/agent-release-notes/net-release-notes">New Relic .NET Agent 8.21+<a>
* <a target="_blank" href="https://docs.newrelic.com/docs/agents/net-agent/net-agent-api" target="_blank">New Relic .NET Agent API 8.21+</a>

## Support

Should you need assistance with New Relic products, you are in good hands with several support diagnostic tools and support channels.

This [troubleshooting framework](https://discuss.newrelic.com/t/troubleshooting-frameworks/108787) steps you through common troubleshooting questions. 

If the issue has been confirmed as a bug or is a Feature request, please file a Github issue.

**Support Channels**

* [New Relic Documentation](https://docs.newrelic.com/docs/agents/net-agent): Comprehensive guidance for using our agent
* [New Relic Community](https://forum.newrelic.com/): The best place to engage in troubleshooting questions
* [New Relic Developer](https://developer.newrelic.com/): Resources for building a custom observability applications
* [New Relic University](https://learn.newrelic.com/): A range of online training for New Relic users of every level
* [New Relic Technical Support](https://support.newrelic.com/) 24/7/365 ticketed support. Read more about our [Technical Support Offerings](https://docs.newrelic.com/docs/licenses/license-information/general-usage-licenses/support-plan). 

## Privacy
At New Relic we take your privacy and the security of your information seriously, and are committed to protecting your information. We must emphasize the importance of not sharing personal data in public forums, and ask all users to scrub logs and diagnostic information for sensitive information, whether personal, proprietary, or otherwise.

We define “Personal Data” as any information relating to an identified or identifiable individual, including, for example, your name, phone number, post code or zip code, Device ID, IP address and email address.

Please review [New Relic’s General Data Privacy Notice](https://newrelic.com/termsandconditions/privacy) for more information.


## Contributing
We encourage your contributions to improve New Relic's .NET monitoring products! Keep in mind that when you submit your pull request, you'll need to sign the CLA via the click-through using CLA-Assistant. You only have to sign the CLA one time per project.
To execute our corporate CLA, which is required if your contribution is on behalf of a company, or if you have any questions, please drop us an email at open-source@newrelic.com.

**A note about vulnerabilities**

As noted in our [security policy](https://github.com/newrelic/newrelic-logenricher-dotnet/security/policy), New Relic is committed to the privacy and security of our customers and their data. We believe that providing coordinated disclosure by security researchers and engaging with the security community are important means to achieve our security goals.

If you believe you have found a security vulnerability in this project or any of New Relic's products or websites, we welcome and greatly appreciate you reporting it to New Relic through [HackerOne](https://hackerone.com/newrelic).

If you would like to contribute to this project, please review [these guidelines](CONTRIBUTING.md).

To [all contributors](https://github.com/newrelic/newrelic-logenricher-dotnet/graphs/contributors), we thank you!  Without your contribution, this project would not be what it is today.  We also host a community project page dedicated to
the [New Relic .NET Logging Extensions](https://opensource.newrelic.com/projects/newrelic/newrelic-logenricher-dotnet).
   

## License
This project is licensed under the [Apache 2.0](http://apache.org/licenses/LICENSE-2.0.txt) License.

This project also uses source code from third-party libraries. You can find full details on which libraries are used and the terms under which they are licensed in the third-party notices document.

