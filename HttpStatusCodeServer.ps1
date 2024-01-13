param(
        [parameter(Mandatory=$true)]
        [int]$StatusCodeToReturn,
        [parameter(Mandatory=$false)]
        [int]$ListeningPort = 9090
    )

$httpListener = New-Object System.Net.HttpListener
$httpListener.Prefixes.Add("http://localhost:$ListeningPort/")
$httpListener.Start()

# Log ready message to terminal 
if ($httpListener.IsListening) {
    write-host "<*> HTTP Response Server Ready!  "
    write-host "<*> Listening on $($httpListener.Prefixes)"
}

while($httpListener.IsListening) {
    write-host "<*> Waiting for request..."
    $context = $httpListener.GetContext()
    $context.Response.StatusCode = $StatusCodeToReturn
    Write-Host "<*> Responding to request from $($context.Request.RemoteEndPoint)"
    Write-Host "<*> Request URL $($context.Request.Url)"
    Write-Host "<*> Request User-Agent $($context.Request.UserAgent)"
    Write-Host "<*> Request Referer: $($context.Request.UrlReferrer)"
    $context.Response.Close()
    write-host "<*> Response of HTTP $StatusCodeToReturn sent!"
}
