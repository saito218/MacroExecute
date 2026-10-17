param(
    [Parameter(Mandatory=$true)][string]$ReferenceExe,
    [Parameter(Mandatory=$true)][string]$RebuiltExe
)
$ErrorActionPreference='Stop'
function Get-BuildFingerprint([string]$Path) {
    $resolved=(Resolve-Path -LiteralPath $Path).Path
    [byte[]]$bytes=[IO.File]::ReadAllBytes($resolved)
    function U16([int]$offset){if($offset -lt 0 -or $offset+2 -gt $bytes.Length){throw 'Invalid PE offset'};[BitConverter]::ToUInt16($bytes,$offset)}
    function U32([int]$offset){if($offset -lt 0 -or $offset+4 -gt $bytes.Length){throw 'Invalid PE offset'};[BitConverter]::ToUInt32($bytes,$offset)}
    if((U16 0) -ne 0x5a4d){throw 'Not a Windows executable'}
    $pe=[int](U32 0x3c)
    if((U32 $pe) -ne 0x4550){throw 'Invalid PE signature'}
    $sectionCount=U16 ($pe+6);$optionalSize=U16 ($pe+20);$optional=$pe+24;$magic=U16 $optional
    $directories=if($magic -eq 0x10b){$optional+96}elseif($magic -eq 0x20b){$optional+112}else{throw 'Unknown PE format'}
    $sections=$optional+$optionalSize
    function FileOffset([uint32]$rva){for($index=0;$index -lt $sectionCount;$index++){$section=$sections+40*$index;$start=U32 ($section+12);$length=[Math]::Max((U32 ($section+8)),(U32 ($section+16)));if($rva -ge $start -and $rva-$start -lt $length){return [int]((U32 ($section+20))+$rva-$start)}};throw 'Unmapped PE RVA'}
    $cli=FileOffset (U32 ($directories+14*8));$metadata=FileOffset (U32 ($cli+8))
    if((U32 $metadata) -ne 0x424a5342){throw 'Invalid managed metadata'}
    $versionLength=U32 ($metadata+12);$streams=$metadata+16+([int][Math]::Ceiling($versionLength/4.0))*4;$count=U16 ($streams+2);$entry=$streams+4;$guidOffset=-1;$stringsOffset=-1;$stringsLength=0
    for($index=0;$index -lt $count;$index++){
        $offset=U32 $entry;$length=U32 ($entry+4);$nameStart=$entry+8;$end=$nameStart
        while($end -lt $bytes.Length -and $bytes[$end] -ne 0){$end++}
        if($end -ge $bytes.Length){throw 'Invalid stream name'}
        $name=[Text.Encoding]::ASCII.GetString($bytes,$nameStart,$end-$nameStart)
        if($name -eq '#GUID'){if($length -ne 16){throw 'Expected a single compiler module GUID; refusing broader normalization'};$guidOffset=$metadata+$offset}
        if($name -eq '#Strings'){$stringsOffset=$metadata+$offset;$stringsLength=$length}
        $entry=$nameStart+([int][Math]::Ceiling(($end-$nameStart+1)/4.0))*4
    }
    if($guidOffset -lt 0 -or $guidOffset+16 -gt $bytes.Length){throw 'Module GUID not found'}
    if($stringsOffset -lt 0 -or $stringsOffset+$stringsLength -gt $bytes.Length){throw 'Invalid strings heap'}
    [byte[]]$moduleBytes=$bytes[$guidOffset..($guidOffset+15)]
    $moduleId=(New-Object Guid (,$moduleBytes)).ToString('D').ToUpperInvariant()
    $prefix='<PrivateImplementationDetails>{'
    $needle=[Text.Encoding]::ASCII.GetBytes($prefix+$moduleId+'}')
    $identityOffsets=@()
    for($position=$stringsOffset;$position -le $stringsOffset+$stringsLength-$needle.Length-1;$position++){
        if($position -gt $stringsOffset -and $bytes[$position-1] -ne 0){continue}
        $equal=$true
        for($n=0;$n -lt $needle.Length;$n++){if($bytes[$position+$n] -ne $needle[$n]){$equal=$false;break}}
        if($equal -and $bytes[$position+$needle.Length] -eq 0){$identityOffsets+=($position+$prefix.Length)}
    }
    if($identityOffsets.Count -gt 1){throw 'Unexpected duplicate compiler identity names'}
    $hash=[Security.Cryptography.SHA256]::Create()
    try{$raw=([BitConverter]::ToString($hash.ComputeHash($bytes))).Replace('-','').ToLowerInvariant();foreach($identityOffset in $identityOffsets){[Array]::Clear($bytes,$identityOffset,36)};[Array]::Clear($bytes,$pe+8,4);[Array]::Clear($bytes,$guidOffset,16);$normalized=([BitConverter]::ToString($hash.ComputeHash($bytes))).Replace('-','').ToLowerInvariant()}finally{$hash.Dispose()}
    [pscustomobject]@{File=[IO.Path]::GetFileName($resolved);Size=$bytes.Length;SHA256=$raw;NormalizedSHA256=$normalized}
}
$reference=Get-BuildFingerprint $ReferenceExe
$rebuilt=Get-BuildFingerprint $RebuiltExe
$matches=$reference.Size -eq $rebuilt.Size -and $reference.NormalizedSHA256 -eq $rebuilt.NormalizedSHA256
[pscustomobject]@{Reference=$reference;Rebuilt=$rebuilt;Match=$matches;IgnoredFields=@('PE build timestamp (4 bytes)','Single compiler module GUID (16 bytes)','Exact compiler-generated PrivateImplementationDetails GUID (36 ASCII bytes, when present)')} | ConvertTo-Json -Depth 4
if(-not $matches){throw 'Builds differ beyond compiler timestamp/module identity. Investigate before distributing.'}
