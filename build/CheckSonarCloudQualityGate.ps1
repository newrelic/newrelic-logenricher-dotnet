# This script checks for Quality Gate status for a SonarQube Project
[CmdletBinding()]
Param(
    # Define Sonar Token
    [Parameter (Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [String] $SonarToken,

    # Define SonarQube Server URI
    [Parameter (Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [String] $SonarServerName,

    # Define Project Key
    [Parameter (Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [String] $SonarProjectKey,

    # Define Project Key
    [Parameter (Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [int] $PullRequestNumber
    
)

$Token = [System.Text.Encoding]::UTF8.GetBytes($SonarToken + ":")
$TokenInBase64 = [System.Convert]::ToBase64String($Token)
 
$basicAuth = [string]::Format("Basic {0}", $TokenInBase64)
$Headers = @{ Authorization = $basicAuth }

$qryParams = @{ projectKey = $SonarProjectKey; pullRequest = $PullRequestNumber}

# $QualityGateResult = Invoke-RestMethod -Method Get -Uri http://$SonarServerName/api/qualitygates/project_status?projectKey=($SonarProjectKey)&amp;pullRequest=62 -Headers $Headers
$QualityGateResult = Invoke-RestMethod -Method Get -Uri http://$SonarServerName/api/qualitygates/project_status -Headers $Headers -Body $qryParams

$QualityGateResult | ConvertTo-Json | Write-Host
 
 Write-Host ""
 Write-Host "------------------------------------------------------------------------------------------------------------------------------"
 Write-Host "Use the following link will view the output of SonarCloud analysis"
 Write-Host "https://$SonarServerName/dashboard?id=$SonarProjectKey&pullRequest=$PullRequestNumber"
 Write-Host "------------------------------------------------------------------------------------------------------------------------------"
 Write-Host ""

if ($QualityGateResult.projectStatus.status -eq "OK"){
    Write-Host "SonarCloud Quality Gate Succeeded!!!"
}
else{

    

    throw "SonarCloud Quality Gate Failed"
}