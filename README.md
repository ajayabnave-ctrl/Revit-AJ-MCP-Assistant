# Revit-AJ-MCP-Assistant 🏢🤖

An open-source AI-Powered Autodesk Revit Automation Plugin and Model Context Protocol (MCP) Server ecosystem built specifically for **Revit 2025 onwards (.NET 8.0)**.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Revit API](https://img.shields.io/badge/Revit%20API-2025%2B%20(Revit%202025%2F2026)-orange)
![Python](https://img.shields.io/badge/Python-3.11%2B-blue)
![C#](https://img.shields.io/badge/C%23-.NET%208.0-green)
![MCP Tools](https://img.shields.io/badge/MCP%20Tools-27%20Tools%20Active-brightgreen)

---

## 🌟 Overview

`Revit-AJ-MCP-Assistant` enables natural language AI interaction (via Claude Desktop, ChatGPT, Gemini, and other MCP-compliant agents) with Autodesk Revit. It uses a **two-tier architecture**:

1. **C# Revit Add-In (.NET 8.0)**: An in-process plugin featuring an embedded HTTP server (`HttpListener`), dynamic JSON task dispatcher, and `ExternalEvent` task queue to safely execute BIM actions on the Revit UI main thread. Includes a ribbon UI with status monitor and **One-Click Server Restart**.
2. **Python MCP Server**: A high-performance FastMCP server exposing 27 structured MCP tools to AI assistants, routing commands to Revit over local JSON REST endpoints (`http://localhost:8080/revit/v1/`).

---

## 🏗️ Architecture Blueprint & Generic Execution Layer

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
|               ||                                              ||        |
|               \/                                              \/        |
|  +---------------------------+                          +------------+  |
|  | Dynamic JSON Dispatcher   | -----------------------> | BIM Model  |  |
|  | (GenericElementBuilder)   |                          +------------+  |
|  +---------------------------+                                          |
+-------------------------------------------------------------------------+
```

---

## 🛠️ Full Suite of 27 Supported MCP AI Tools

| Category | Tool Name | Description |
| :--- | :--- | :--- |
| 🔌 **Connection & Testing** | `say_hello` | Display a greeting dialog in Revit UI |
| | `ping_revit_status` | Check connection status with Autodesk Revit |
| | `get_revit_document_info` | Get title and modification status of active model |
| 👁️ **View & Selection** | `get_current_view_info` | Get active view name, type, scale, and level |
| | `get_current_view_elements` | Get all elements visible in the active view |
| | `get_selected_elements` | Get currently selected elements in Revit UI |
| | `get_available_family_types` | Get available loaded family types in project |
| 📊 **Quantities & Stats** | `get_material_quantities` | Calculate material quantities and takeoffs |
| | `analyze_model_statistics` | Analyze model complexity with element counts |
| 🧱 **Universal Modeling** | `create_revit_element` | Universal dynamic builder (Wall, Room, Floor, Door, Window, Sheet) |
| | `create_point_based_element` | Create point-based elements (doors, windows, furniture, lighting) |
| | `create_line_based_element` | Create line-based elements (walls, beams, pipes, ducts) |
| | `create_surface_based_element` | Create surface-based elements (floors, ceilings, roofs) |
| | `create_revit_wall` | Create standard wall from start to end coordinates |
| | `create_revit_wall_advanced` | Create wall with height, top constraint, wall type, structural flag |
| 📑 **Schedules & Sheets** | `create_revit_lighting_fixture_schedule` | Dedicated Lighting Fixture Schedule with MEP fields & sorting |
| | `create_revit_schedule_advanced` | Custom schedule creation with custom fields, sorting, itemization |
| | `create_revit_schedule` | View schedule creation for any category |
| | `list_revit_schedules` | List all view schedules in active project |
| | `list_revit_sheets` | List all drawing sheets in active project |
| | `create_revit_sheet` | Create new sheet with title block |
| 📐 **Grids, Levels & Rooms** | `create_grid` | Create grid lines with coordinates and labels |
| | `create_level` | Create levels at specified elevations in meters |
| | `create_room` | Create and place rooms at specified locations |
| | `create_structural_framing_system` | Create structural beam framing system |
| 🏷️ **Annotations & Operations** | `tag_all_walls` | Tag all walls in active view |
| | `tag_all_rooms` | Tag all rooms in active view |
| | `create_dimensions` | Create dimension annotations in active view |
| | `delete_element` | Delete elements by ID |
| | `operate_element` | Operate on elements in UI (select, hide) |
| | `color_elements` | Apply RGB graphic color overrides to elements |
| 💾 **Data Export & Store** | `export_room_data` | Export all room data (areas, names, levels) as JSON |
| | `store_project_data` | Store project metadata in local JSON database |
| | `store_room_data` | Store room metadata in local JSON database |
| | `query_stored_data` | Query stored project and room metadata |
| | `send_code_to_revit` | Send C# payload / script to Revit to execute |

---

## 📁 Repository & File Structure

```text
C:\Users\SHREE\Revit_Addins\Revit_AJ_MCP\
├── docs/                   # Documentation & Workflow Guides
├── src/
│   ├── RevitAddin/         # C# Add-in Source (.NET 8.0 for Revit 2025+)
│   │   ├── App.cs          # IExternalApplication Entry Point & Ribbon UI
│   │   ├── Commands/       # ShowServerStatusCommand & RestartServerCommand
│   │   ├── Handlers/       # Thread-safe ExternalEvent Dispatcher
│   │   ├── Server/         # Embedded HttpListener REST API Server
│   │   ├── Services/       # Geometry, View, Analysis, Manipulation, Schedule, Workset, Storage Services
│   │   ├── Revit-AJ-MCP-Assistant.csproj
│   │   └── Revit-AJ-MCP-Assistant.addin
│   │
│   ├── MCPServer/          # Python Model Context Protocol Server
│   │   ├── main.py         # FastMCP Server Entry Point (27 Tools)
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

### STEP 1: Prerequisites & Software Setup
Ensure the target machine has the following installed:
1. **Autodesk Revit 2025 or 2026** (Installed at default path `C:\Program Files\Autodesk\Revit 2025\`).
2. **.NET 8.0 SDK** (Installed on Windows x64).
3. **Python 3.11+** (With `pip` added to System PATH).
4. **Visual Studio 2022** (Optional, with *.NET Desktop Development* workload).

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

---

### STEP 3: Deploy Add-In to Autodesk Revit
1. Create the Revit 2025 Add-ins directory if needed:
   ```powershell
   New-Item -ItemType Directory -Force -Path "$env:APPDATA\Autodesk\Revit\Addins\2025"
   ```
2. Copy compiled DLL and manifest to Revit's Add-ins folder:
   ```powershell
   Copy-Item -Path "src\RevitAddin\bin\Release\net8.0-windows\Revit-AJ-MCP-Assistant.dll" -Destination "$env:APPDATA\Autodesk\Revit\Addins\2025\Revit-AJ-MCP-Assistant.dll" -Force
   Copy-Item -Path "src\RevitAddin\bin\Release\net8.0-windows\Revit-AJ-MCP-Assistant.addin" -Destination "$env:APPDATA\Autodesk\Revit\Addins\2025\Revit-AJ-MCP-Assistant.addin" -Force
   ```

---

### STEP 4: Start Revit & Verify Ribbon UI Buttons
1. Launch **Autodesk Revit 2025** or **2026**.
2. Open an existing project or create a new architectural model.
3. Observe the top Ribbon bar: Look for **`AJ MCP Assistant`** tab.
4. Available Ribbon buttons:
   - 🟢 **`MCP Server Status`**: Check connection status (`http://localhost:8080/revit/v1/`).
   - 🔄 **`Restart MCP Server`**: One-click restart of the embedded HTTP listener to clean sockets and refresh AI connections without restarting Revit.

---

### STEP 5: Set Up the Python MCP Server
1. Navigate to the MCP Server folder:
   ```powershell
   cd C:\Users\SHREE\Revit_Addins\Revit_AJ_MCP\src\MCPServer
   ```
2. Create and activate Python virtual environment:
   ```powershell
   python -m venv .venv
   .\.venv\Scripts\Activate.ps1
   ```
3. Install required Python packages:
   ```powershell
   pip install -r requirements.txt
   ```

---

### STEP 6: Connect to Claude Desktop (Packaged Store App)
Add the configuration to your Claude Desktop config file:

**Configuration File Location**:
`C:\Users\SHREE\AppData\Local\Packages\Claude_pzs8sxrjxfjjc\LocalCache\Roaming\Claude\claude_desktop_config.json`

```json
{
  "mcpServers": {
    "revit-aj-mcp": {
      "command": "C:\\Users\\SHREE\\Revit_Addins\\Revit_AJ_MCP\\src\\MCPServer\\.venv\\Scripts\\python.exe",
      "args": [
        "C:\\Users\\SHREE\\Revit_Addins\\Revit_AJ_MCP\\src\\MCPServer\\main.py"
      ]
    }
  }
}
```

---

## 👨‍💻 Author & Version Control

- **GitHub Repository**: [ajayabnave-ctrl/Revit-AJ-MCP-Assistant](https://github.com/ajayabnave-ctrl/Revit-AJ-MCP-Assistant)
- **License**: MIT License - Free for open-source and commercial use.
