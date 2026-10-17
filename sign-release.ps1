param(
    [Parameter(Mandatory=$true)][string]$Path,
    [Parameter(Mandatory=$true)][string]$CertificateThumbprint,
    [Parameter(Mandatory=$true)][uri]$TimestampServer
)
$ErrorActionPreference = 'Stop'
if (-not $TimestampServer.IsAbsoluteUri -or $TimestampServer.Scheme -notin @('http','https')) { throw 'Use an absolute HTTP(S) timestamp server URL provided by your certificate issuer.' }
$releasePath = (Resolve-Path -LiteralPath $Path).Path
if ([IO.Path]::GetExtension($releasePath) -ne '.exe') { throw 'Choose a built MacroExecute executable or exported player.' }
$thumbprint = $CertificateThumbprint.Replace(' ','')
if ($thumbprint -notmatch '^[0-9a-fA-F]{40}$') { throw 'Enter a certificate thumbprint, not a password or private key.' }
$certificate = Get-Item -LiteralPath "Cert:\CurrentUser\My\$thumbprint"
if (-not $certificate.HasPrivateKey -or $certificate.NotAfter -le (Get-Date) -or $certificate.NotBefore -gt (Get-Date)) { throw 'An unexpired code-signing certificate with its private key is required.' }
if ('1.3.6.1.5.5.7.3.3' -notin @($certificate.EnhancedKeyUsageList | ForEach-Object { [string]$_.ObjectId })) { throw 'The certificate must permit code signing.' }
$signingTemporary = Join-Path ([IO.Path]::GetDirectoryName($releasePath)) ('.signing-'+[guid]::NewGuid().ToString('N')+'.exe')
try {
    Copy-Item -LiteralPath $releasePath -Destination $signingTemporary
    $signature = Set-AuthenticodeSignature -LiteralPath $signingTemporary -Certificate $certificate -HashAlgorithm SHA256 -TimestampServer $TimestampServer.AbsoluteUri
    if ($signature.Status -ne 'Valid') { throw "Signing failed: $($signature.StatusMessage)" }
    $verification = Get-AuthenticodeSignature -LiteralPath $signingTemporary
    if ($verification.Status -ne 'Valid' -or $verification.SignerCertificate.Thumbprint -ne $certificate.Thumbprint) { throw 'Signature verification failed; the original file is unchanged.' }
    [IO.File]::Replace($signingTemporary,$releasePath,$null)
    Write-Output "Signed and verified $releasePath"
} finally {
    if (Test-Path -LiteralPath $signingTemporary) { Remove-Item -LiteralPath $signingTemporary -Force }
}
