# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Add `logger.name` to NLog and Log4Net
- A new property named `logger.name` is added to the New Relic layouts for NLog and Log4Net, with the value of the name of the logger that created the log event.

## [Log4Net_v1.0.2] - 2020-10-28
### Bugfix release
- Fixes [Issue #84](https://github.com/newrelic/newrelic-logenricher-dotnet/issues/84) where the log4net log enricher incorrectly extracts log level value to report to New Relic.

## [Log4Net_v1.0.1, NLog_v1.0.2, Serilog_v1.0.1] - 2020-03-02
### Bugfix release
- Change the user property prefix from "Message Properties." to "Message.Properties." to fix an issue with search in the logging UI.

## [Log4Net_v1.0.0] - 2020-02-04
### Initial Release supporting log4net
- Adds `NewRelic.LogEnrichers.Log4Net.NewRelicAppender` and `NewRelic.LogEnrichers.Log4Net.NewRelicLayout`.
- Adds sample application
- Adds implementation documentation.

## [NLog_v1.0.1] - 2020-01-24
### Bugfix release
- Depend on NLog 4.5.11 instead of 4.5.0.
- Set the default value of `MaxRecursionLimit` in `NewRelicJsonLayout` to 1.

## [NLog_v1.0.0] - 2020-01-16
### Initial Release supporting NLog
- Adds `NewRelic.LogEnrichers.NLog.NewRelicJsonLayout`.
- Adds sample application
- Adds implementation documentation.

## [Serilog_v1.0.0] - 2019-11-14
### Initial Release supporting Serilog
- Adds `NewRelic.LogEnrichers.Serilog.NewRelicEnricher` and `NewRelic.LogEnrichers.Serilog.NewRelicFormatter`.
- Adds sample application
- Adds implementation documentation.

[Unreleased]: https://github.com/newrelic/newrelic-logenricher-dotnet/compare/Log4Net_v1.0.2...HEAD
[Log4Net_v1.0.2]: https://github.com/newrelic/newrelic-logenricher-dotnet/compare/Log4Net_v1.0.1...Log4Net_v1.0.2
[Log4Net_v1.0.1, NLog_v1.0.2, Serilog_v1.0.1]: https://github.com/newrelic/newrelic-logenricher-dotnet/compare/Log4Net_v1.0.0...Serilog_v1.0.1
[Log4Net_v1.0.0]: https://github.com/newrelic/newrelic-logenricher-dotnet/compare/f354ce5...Log4Net_v1.0.0
[NLog_v1.0.1]: https://github.com/newrelic/newrelic-logenricher-dotnet/compare/NLog_v1.0.0...NLog_v1.0.1
[NLog_v1.0.0]: https://github.com/newrelic/newrelic-logenricher-dotnet/compare/60940cd...NLog_v1.0.0
[Serilog_v1.0.0]: https://github.com/newrelic/newrelic-logenricher-dotnet/compare/33cded7...Serilog_v1.0.0


