import asyncio
import sys

# Support both official 'mcp' SDK and standalone 'fastmcp' package
try:
    from mcp.server.fastmcp import FastMCP
except ImportError:
    try:
        from fastmcp import FastMCP
    except ImportError:
        raise ImportError(
            "Neither 'mcp' nor 'fastmcp' module is installed. "
            "Please run: pip install mcp fastmcp httpx pydantic"
        )

import tools

# Initialize MCP Server for Revit 2025+ (.NET 8.0)
mcp = FastMCP("Revit-AJ-MCP-Assistant")

@mcp.tool()
async def ping_revit_status() -> str:
    """Check connection status with Autodesk Revit."""
    res = await tools.ping_revit()
    return str(res)

@mcp.tool()
async def get_revit_document_info() -> str:
    """Get title and modification status of the active Revit model."""
    res = await tools.get_active_document_info()
    return str(res)

@mcp.tool()
async def create_revit_wall(start_x: float = 0.0, start_y: float = 0.0, end_x: float = 20.0, end_y: float = 0.0, level_name: str = "Level 1") -> str:
    """Create a wall in the active Revit model from start (X, Y) to end (X, Y) coordinates."""
    res = await tools.create_wall_tool(start_x, start_y, end_x, end_y, level_name)
    return str(res)

@mcp.tool()
async def list_revit_sheets() -> str:
    """List all documentation sheets in the active Revit project."""
    res = await tools.list_sheets_tool()
    return str(res)

@mcp.tool()
async def create_revit_sheet(sheet_number: str = "A101", sheet_name: str = "AI AUTOMATED SHEET") -> str:
    """Create a new sheet with a title block in the active Revit model."""
    res = await tools.create_sheet_tool(sheet_number, sheet_name)
    return str(res)

if __name__ == "__main__":
    mcp.run()
