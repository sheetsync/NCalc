param(
    [Parameter(Mandatory = $true)]
    [string] $PackageSource,

    [Parameter(Mandatory = $true)]
    [string] $PackageVersion,

    [string] $SmokeRoot = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "ncalc-framework-consumer-smoke-" + [System.Guid]::NewGuid().ToString("N"))
)

$ErrorActionPreference = "Stop"

function Require-Command {
    param([Parameter(Mandatory = $true)][string] $Name)

    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if ($null -eq $command) {
        throw "Required command not found: $Name"
    }
}

function Write-SmokeProject {
    param([Parameter(Mandatory = $true)][string] $ProjectDirectory)

    New-Item -ItemType Directory -Force -Path $ProjectDirectory | Out-Null

    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net48</TargetFramework>
    <AssemblyName>NCalc.FrameworkConsumerSmoke</AssemblyName>
    <RootNamespace>NCalc.FrameworkConsumerSmoke</RootNamespace>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>disable</Nullable>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="NCalc" Version="$PackageVersion" />
    <PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies.net48" Version="1.0.3" PrivateAssets="all" />
  </ItemGroup>
</Project>
"@ | Set-Content -NoNewline -Encoding UTF8 -Path (Join-Path $ProjectDirectory "NCalc.FrameworkConsumerSmoke.csproj")

    @"
using System;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using NCalc;

namespace NCalc.FrameworkConsumerSmoke
{
    internal static class Program
    {
        private static int Main()
        {
            object result = new Expression("2 * (3 + 5)").Evaluate();
            if (!object.Equals(16, result))
            {
                throw new InvalidOperationException("Unexpected expression result: " + result);
            }

            NCalcLexer lexer = new NCalcLexer(new ANTLRStringStream("1 + 2"));
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            NCalcParser parser = new NCalcParser(tokens);
            NCalcParser.ncalcExpression_return parserResult = parser.ncalcExpression();
            CommonTree tree = parserResult.Tree as CommonTree;

            if (tree == null)
            {
                throw new InvalidOperationException("Expected parser to return a CommonTree.");
            }

            Console.WriteLine("NCalc .NET Framework consumer smoke passed: " + result);
            return 0;
        }
    }
}
"@ | Set-Content -NoNewline -Encoding UTF8 -Path (Join-Path $ProjectDirectory "Program.cs")
}

function Invoke-Smoke {
    Require-Command "dotnet"

    $package = Get-ChildItem -Path $PackageSource -Filter "NCalc.$PackageVersion.nupkg" -File | Select-Object -First 1
    if ($null -eq $package) {
        throw "NCalc $PackageVersion package not found in $PackageSource"
    }

    $projectDirectory = Join-Path $SmokeRoot "NCalc.FrameworkConsumerSmoke"
    $packagesDirectory = Join-Path $SmokeRoot "packages"
    Write-SmokeProject -ProjectDirectory $projectDirectory

    $projectPath = Join-Path $projectDirectory "NCalc.FrameworkConsumerSmoke.csproj"
    dotnet restore $projectPath --source $PackageSource --source "https://api.nuget.org/v3/index.json" /p:RestorePackagesPath=$packagesDirectory
    dotnet build $projectPath --configuration Release --no-restore

    $executablePath = Join-Path $projectDirectory "bin/Release/net48/NCalc.FrameworkConsumerSmoke.exe"
    if (!(Test-Path $executablePath)) {
        throw "Expected smoke executable not found: $executablePath"
    }

    & $executablePath
}

Invoke-Smoke
