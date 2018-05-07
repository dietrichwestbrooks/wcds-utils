[CmdletBinding()]
param(
    [Parameter(Mandatory=$true, Position=0)]$inputPath,
    [Parameter(Mandatory=$false, Position=1)][ValidateNotNullOrEmpty()][string]$outputFile = ".\ATLS_Server_Build_1.0.15.1-380 System Files.csv",
    [Parameter(Mandatory=$false, Position=2)][ValidateNotNullOrEmpty()][string]$descXmlPath = ".\Descriptions.xml"
)

#$inputPath = "C:\Users\dwestbro\Downloads\Aristocrat\ATLS\Builds\1.0.15.1-380-deploy"

$xml = Get-Content -Path $descXmlPath

$descriptions = $xml.descriptions.add | select pattern, @{Name="description";Expression={$_.InnerText}}

$descriptions = @{}

foreach ($desc in $xml.descriptions.add)
{
    $descriptions[$desc.pattern] = $desc.InnerText
}

$systemFiles = New-Object System.Collections.ArrayList

Get-ChildItem -Recurse -Path $inputPath -Filter "manifest.txt" | % {
    $csv = Import-Csv $_.FullName -Header @("Name","Level","Type","Hash")

    $csv | % {
        if (($_.Name -notmatch "^(?:appsettings|App|Web)(?:\.\w+)*?\.(?:json|config)$") -and ($_.Name -notmatch "^.*\.config$")) {
            $file = [PSCustomObject]@{
                        Name = $_.Name
                        Description = ""
                        Hash = $_.Hash
                        }

            $i = [bool]($systemFiles | % {$_.Name -eq $file.Name -and $_.Hash -eq $file.Hash} | ? {$_} | select -First 1)

            if (!$i)
            {
                $desc = $descriptions.GetEnumerator() | ? {$file.Name -match "^$($_.Key)`$"} | select Value -First 1

                if ($desc)
                {
                    $file.Description = $desc.Value
                }
                else
                {
                    Write-Output "No description found for $($file.Name)"
                }

                #$file.Description

                $systemFiles.Add($file) | Out-Null
            }
        }
    }
}

$systemFiles | Export-Csv -Path $outputFile -Delimiter "," -NoTypeInformation -Header @("System File","Description","Manifest SHA1")
