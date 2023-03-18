#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["LogEnricherTestPlayground/LogEnricherTestPlayground.csproj", "LogEnricherTestPlayground/"]
RUN dotnet restore "LogEnricherTestPlayground/LogEnricherTestPlayground.csproj"
COPY . .
WORKDIR "/src/LogEnricherTestPlayground"
RUN dotnet build "LogEnricherTestPlayground.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LogEnricherTestPlayground.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final

# Install the agent
RUN apt-get update && apt-get install -y wget ca-certificates gnupg \
&& echo 'deb http://apt.newrelic.com/debian/ newrelic non-free' | tee /etc/apt/sources.list.d/newrelic.list \
&& wget https://download.newrelic.com/548C16BF.gpg \
&& apt-key add 548C16BF.gpg \
&& apt-get update \
&& apt-get install -y newrelic-dotnet-agent

# Enable the agent
# IMPORTANT: Get your own license key, dawg
ENV CORECLR_ENABLE_PROFILING=1 \
CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A} \
CORECLR_NEWRELIC_HOME=/usr/local/newrelic-dotnet-agent \
CORECLR_PROFILER_PATH=/usr/local/newrelic-dotnet-agent/libNewRelicProfiler.so \
NEW_RELIC_LICENSE_KEY=!!!GET_YOUR_OWN_LICENSE_KEY!!! \
NEW_RELIC_APP_NAME=LogEnricherPlayground \
NEW_RELIC_HOST=staging-collector.newrelic.com \
NEW_RELIC_APPLICATION_LOGGING_ENABLED=false


# install infra agent
RUN apt-get install curl -y
RUN curl -fsSL https://download.newrelic.com/infrastructure_agent/gpg/newrelic-infra.gpg | gpg --dearmor -o /etc/apt/trusted.gpg.d/newrelic-infra.gpg 
RUN echo "deb https://download.newrelic.com/infrastructure_agent/linux/apt bullseye main" | tee -a /etc/apt/sources.list.d/newrelic-infra.list 
RUN apt-get update 
RUN apt-get install systemctl -y 
RUN apt-get install newrelic-infra -y 

# Setup infra agent license key
# IMPORTANT: Get your own license key, dawg
RUN echo "license_key: !!!GET_YOUR_OWN_LICENSE_KEY!!!" | tee -a /etc/newrelic-infra.yml 
RUN echo "staging: true" | tee -a /etc/newrelic-infra.yml 
RUN echo "log:" | tee -a /etc/newrelic-infra.yml 
RUN echo "  level: debug" | tee -a /etc/newrelic-infra.yml 
RUN echo "  file: /var/log/infra-logfile.log" | tee -a /etc/newrelic-infra.yml 

# Setup log forwarding
RUN echo "logs:" | tee -a /etc/newrelic-infra/logging.d/logging.yml 
RUN echo "  - name: basic-file" | tee -a /etc/newrelic-infra/logging.d/logging.yml 
RUN echo "    file: /var/log/NetServiceLogfile.log" | tee -a /etc/newrelic-infra/logging.d/logging.yml
RUN echo "    attributes:" | tee -a /etc/newrelic-infra/logging.d/logging.yml
# This works to add custom attributes to the log messages, but I'm not sure what to add here yet
#RUN echo "      test-custom-attribute-key: test-custom-attribute-value" | tee -a /etc/newrelic-infra/logging.d/logging.yml

# IMPORTANT: You must manually run `systemctl start newrelic-infra` once the container starts when run within visual studio.
#RUN systemctl start newrelic-infra 
#RUN systemctl status newrelic-infra

RUN echo "#!/bin/bash" | tee -a /app/start.sh 
RUN echo "systemctl start newrelic-infra" | tee -a /app/start.sh 
RUN echo "dotnet LogEnricherTestPlayground.dll" | tee -a /app/start.sh
RUN chmod +x /app/start.sh

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./start.sh"]
