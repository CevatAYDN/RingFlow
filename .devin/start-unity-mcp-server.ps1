# Start the Unity MCP HTTP server on the default endpoint (http://127.0.0.1:8080/mcp).
# Run this from a terminal after uv is installed.
$uvPath = Join-Path $env:USERPROFILE ".local\bin"
if (-not ($env:Path -like "*$uvPath*")) {
    $env:Path = "$uvPath;$env:Path"
}

uvx --from mcpforunityserver mcp-for-unity --transport http --http-url http://127.0.0.1:8080
