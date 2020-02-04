# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
