# Revit-AJ-MCP-Assistant 🏢🤖

An open-source AI-Powered Autodesk Revit Automation Plugin and Model Context Protocol (MCP) Server ecosystem built specifically for **Revit 2025 onwards (.NET 8.0)**.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Revit API](https://img.shields.io/badge/Revit%20API-2025%2B%20(Revit%202025%2F2026)-orange)
![Python](https://img.shields.io/badge/Python-3.11%2B-blue)
![C#](https://img.shields.io/badge/C%23-.NET%208.0-green)

---

## 🌟 Overview

`Revit-AJ-MCP-Assistant` enables natural language AI interaction (via Claude, ChatGPT, Gemini, and other MCP-compliant agents) with Autodesk Revit. It uses a **two-tier architecture**:

1. **C# Revit Add-In (.NET 8.0)**: An in-process plugin for Autodesk Revit featuring an embedded HTTP server (`HttpListener`) and `ExternalEvent` task dispatcher to safely execute BIM actions on the Revit UI main thread.
2. **Python MCP Server**: A high-performance Python server exposing standard MCP tools to AI assistants, routing commands to Revit over local JSON REST endpoints (`http://localhost:8080/revit/v1/`).

---

## 🏗️ Architecture Blueprint

```
+------------------+         MCP JSON-RPC        +------------------------+
|  AI Agent / LLM  |  ========================>  | Python MCP Server      |
| (Claude/Gemini)  |                             | (FastMCP / FastAPI)    |
+------------------+                             +------------------------+
                                                             ||
                                                       Local HTTP REST
                                                     http://localhost:8080
                                                             ||
                                                             \/
+-------------------------------------------------------------------------+
| Autodesk Revit Process (Revit 2025 / 2026)                              |
|                                                                         |
|  +---------------------------+       ExternalEvent      +------------+  |
|  | Embedded HttpListener     | -----------------------> | Revit API  |  |
|  | (C# Add-In REST Server)   |  (Thread-Safe Dispatch) | Main Thread|  |
|  +---------------------------+                          +------------+  |
|                                                               ||        |
|                                                               \/        |
|                                                          +------------+ |
|                                                          | BIM Model  | |
|                                                          +------------+ |
+-------------------------------------------------------------------------+
```

---

## 📁 Repository & File Structure

```text
C:\Users\SHREE\Revit_Addins\Revit_AJ_MCP\
├── docs/                   # Documentation & Workflow Guides
├── src/
│   ├── RevitAddin/         # C# Add-in Source (.NET 8.0 for Revit 2025+)
│   │   ├── App.cs          # IExternalApplication Entry Point & Ribbon UI
│   │   ├── Commands/       # Manual Ribbon Commands
│   │   ├── Handlers/       # Thread-safe ExternalEvent Dispatcher
│   │   ├── Server/         # Embedded HttpListener REST API Server
│   │   ├── Services/       # Geometry, Sheet, Parameter Services
│   │   ├── Revit-AJ-MCP-Assistant.csproj
│   │   └── Revit-AJ-MCP-Assistant.addin
│   │
│   ├── MCPServer/          # Python Model Context Protocol Server
│   │   ├── main.py         # FastMCP Server Entry Point
│   │   ├── tools.py        # MCP Tool Definitions
│   │   ├── revit_client.py # HTTP Client communicating with Revit REST listener
│   │   └── requirements.txt
│   │
│   └── Shared/             # Shared JSON Data Transfer Objects (DTOs)
├── Revit-AJ-MCP-Assistant.sln
├── LICENSE                 # MIT License
└── README.md
```

---

## 📖 STEP-BY-STEP IMPLEMENTATION & DEPLOYMENT GUIDE

Follow these steps to deploy and run the app on any target Windows machine:

### STEP 1: Prerequisites & Software Setup
Ensure the target machine has the following installed:
1. **Autodesk Revit 2025 or 2026** (Installed at default path `C:\Program Files\Autodesk\Revit 2025\`).
2. **.NET 8.0 SDK** (Installed on Windows x64).
3. **Python 3.11+** (With `pip` added to System PATH).
4. **Visual Studio 2022** (Optional, with *.NET Desktop Development* workload for editing C# code).

---

### STEP 2: Build the C# Revit Add-In (.NET 8.0)
1. Open PowerShell as Administrator and navigate to the project directory:
   ```powershell
   cd C:\Users\SHREE\Revit_Addins\Revit_AJ_MCP
   ```
2. Build the solution using `dotnet`:
   ```powershell
   dotnet build src/RevitAddin/Revit-AJ-MCP-Assistant.csproj -c Release
   ```
   *(Or open `Revit-AJ-MCP-Assistant.sln` in Visual Studio 2022 and click **Build Solution**).*

---

### STEP 3: Deploy Add-In to Autodesk Revit
1. Create the Revit 2025 Add-ins directory if it doesn't already exist:
   ```powershell
   New-Item -ItemType Directory -Force -Path "$env:APPDATA\Autodesk\Revit\Addins\2025"
   ```
2. Copy the compiled binaries and manifest file into Revit's Add-ins folder:
   ```powershell
   Copy-Item -Path "src\RevitAddin\bin\Release\net8.0-windows\*" -Destination "$env:APPDATA\Autodesk\Revit\Addins\2025\" -Recurse -Force
   ```
3. Verify that the destination folder (`%APPDATA%\Autodesk\Revit\Addins\2025\`) contains:
   - `Revit-AJ-MCP-Assistant.addin`
   - `Revit-AJ-MCP-Assistant.dll`

---

### STEP 4: Start Revit & Verify Connection
1. Launch **Autodesk Revit 2025** or **2026**.
2. Open an existing project or create a new architectural model.
3. Observe the top Ribbon bar: A new tab named **`AJ MCP Assistant`** will be visible.
4. Click the **`MCP Server Status`** button.
5. You should see a status message confirming:
   > *Revit MCP HTTP Listener is active and running on http://localhost:8080/revit/v1/ - Status: READY for Python MCP AI Commands.*

---

### STEP 5: Set Up the Python MCP Server
1. In PowerShell, navigate to the MCP Server folder:
   ```powershell
   cd C:\Users\SHREE\Revit_Addins\Revit_AJ_MCP\src\MCPServer
   ```
2. Create and activate a Python virtual environment:
   ```powershell
   python -m venv .venv
   .\.venv\Scripts\Activate.ps1
   ```
3. Install required Python packages:
   ```powershell
   pip install -r requirements.txt
   ```
4. Start the Python MCP Server:
   ```powershell
   python main.py
   ```

---

### STEP 6: Connect to AI Assistants (Claude Desktop / VS Code / Gemini)
To connect Claude Desktop or your AI Client to the Revit MCP Server, add the following configuration to your MCP config file (e.g., `%APPDATA%\Claude\claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "revit-aj-mcp": {
      "command": "python",
      "args": [
        "C:\\Users\\SHREE\\Revit_Addins\\Revit_AJ_MCP\\src\\MCPServer\\main.py"
      ]
    }
  }
}
```

---

## 🛠️ Available AI Tools & Example Prompts

Once connected, your AI Agent can run commands directly in Revit:

| AI Prompt | Executed Tool | Action in Revit |
| :--- | :--- | :--- |
| *"Check Revit connection status"* | `ping_revit_status` | Returns listener ping status |
| *"Get current document info"* | `get_revit_document_info` | Returns model title & modified state |
| *"Draw a 20ft wall on Level 1"* | `create_revit_wall` | Creates wall in active Revit doc |
| *"Show all drawing sheets"* | `list_revit_sheets` | Returns list of all sheets & viewports |
| *"Create sheet A101 titled AI AUTOMATED SHEET"* | `create_revit_sheet` | Generates new sheet with titleblock |

---

## 👨‍💻 Author & Version Control

- **GitHub Repository**: [ajayabnave-ctrl/Revit-AJ-MCP-Assistant](https://github.com/ajayabnave-ctrl/Revit-AJ-MCP-Assistant)
- **License**: MIT License - Free for open-source and commercial use.
