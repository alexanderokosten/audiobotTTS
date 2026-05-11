$ErrorActionPreference = "Stop"

$hypervisor = (bcdedit /enum | Select-String -Pattern "hypervisorlaunchtype").ToString()
Write-Host $hypervisor

$info = Get-ComputerInfo -Property HyperVisorPresent,HyperVRequirementVirtualizationFirmwareEnabled
Write-Host "HyperVisorPresent: $($info.HyperVisorPresent)"
Write-Host "VirtualizationFirmwareEnabled: $($info.HyperVRequirementVirtualizationFirmwareEnabled)"

docker desktop status
docker info
docker compose config --quiet

Write-Host "Docker is ready for docker compose up -d --build"
