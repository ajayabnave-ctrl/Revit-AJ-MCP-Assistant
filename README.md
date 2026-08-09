# Revit-AJ-MCP-Assistant 🏢🤖

An open-source AI-Powered Autodesk Revit Automation Plugin and Model Context Protocol (MCP) Server ecosystem.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Revit API](https://img.shields.io/badge/Revit%20API-2025%2B%20(Revit%202025%2F2026)-orange)
![Python](https://img.shields.io/badge/Python-3.11%2B-blue)
![C#](https://img.shields.io/badge/C%23-.NET%208.0-green)

---

## 🌟 Overview

`Revit-AJ-MCP-Assistant` is an open-source AI-Powered Autodesk Revit Automation Plugin built specifically for **Revit 2025 onwards using C# .NET 8.0** and Python MCP (Model Context Protocol).

1. **C# Revit Add-In**: An in-process plugin for Autodesk Revit featuring an embedded HTTP server (`HttpListener`) and `ExternalEvent` task dispatcher to safely execute BIM actions on the Revit UI main thread.
2. **Python MCP Server**: A high-performance Python server exposing standard MCP tools to AI assistants, routing commands to Revit over local JSON REST endpoints.

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
| Autodesk Revit Process                                                  |
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

## 📁 Repository Structure

```text
Revit_AJ_MCP/
├── docs/                   # Documentation & Workflow Guides
├── src/
│   ├── RevitAddin/         # C# Add-in Source (.NET 8.0)
│   │   ├── App.cs          # IExternalApplication Entry Point
│   │   ├── Commands/       # Manual Ribbon Commands
│   │   ├── Handlers/       # Thread-safe ExternalEvent Handlers
│   │   ├── Server/         # Embedded HttpListener REST API
│   │   ├── Services/       # Geometry, Sheet, Parameter Services
│   │   └── Revit-AJ-MCP-Assistant.addin
│   │
│   ├── MCPServer/          # Python Model Context Protocol Server
│   │   ├── main.py         # FastMCP Server Entry Point
│   │   ├── tools.py        # MCP Tool Definitions
│   │   ├── revit_client.py # HTTP Client communicating with Revit
│   │   └── requirements.txt
│   │
│   └── Shared/             # Shared JSON Data Transfer Objects (DTOs)
├── Revit-AJ-MCP-Assistant.sln
├── LICENSE                 # MIT License
└── README.md
```

---

## 🚀 Quick Start Guide

### 1. Project Location
Project repository directory: `C:\Users\SHREE\Revit_Addins\Revit_AJ_MCP`

### 2. Building the C# Revit Add-In (.NET 8.0)
1. Open `Revit-AJ-MCP-Assistant.sln` in Visual Studio 2022.
2. Ensure Revit API references (`RevitAPI.dll` and `RevitAPIUI.dll`) point to your local Autodesk Revit 2025/2026 installation folder (`C:\Program Files\Autodesk\Revit 2025\`).
3. Build the solution in `Release` or `Debug` mode.
4. Copy compiled output and `Revit-AJ-MCP-Assistant.addin` manifest into Revit's Add-ins directory:
   `%APPDATA%\Autodesk\Revit\Addins\2025\`

### 3. Setting Up the Python MCP Server
1. Navigate to the MCP Server directory:
   ```bash
   cd C:\Users\SHREE\Revit_Addins\Revit_AJ_MCP\src\MCPServer
   ```
2. Create and activate a virtual environment:
   ```bash
   python -m venv .venv
   .venv\Scripts\activate
   ```
3. Install dependencies and start:
   ```bash
   pip install -r requirements.txt
   python main.py
   ```

---

## 👨‍💻 Author & Version Control

- **GitHub Repository**: [ajayabnave-ctrl/Revit-AJ-MCP-Assistant](https://github.com/ajayabnave-ctrl/Revit-AJ-MCP-Assistant)
- **License**: MIT License
