import asyncio
import sys
from typing import Optional

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
async def create_revit_wall_advanced(
    start_x: float = 0.0, 
    start_y: float = 0.0, 
    end_x: float = 20.0, 
    end_y: float = 0.0, 
    level_name: str = "Level 1",
    height_feet: float = 10.0,
    top_level_name: Optional[str] = None,
    wall_type_name: Optional[str] = None,
    is_structural: bool = False
) -> str:
    """Create a wall with custom height, top constraint, wall type, and structural flag."""
    res = await tools.create_wall_advanced_tool(
        start_x, start_y, end_x, end_y, level_name, height_feet, top_level_name, wall_type_name, is_structural
    )
    return str(res)

@mcp.tool()
async def query_revit_elements(category_name: str = "Plumbing Fixtures", level_name: Optional[str] = None) -> str:
    """Query elements/fixtures in Revit by category (Plumbing Fixtures, Furniture, Doors, Windows, Mechanical Equipment)."""
    res = await tools.query_elements_tool(category_name, level_name)
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

@mcp.tool()
async def list_revit_schedules() -> str:
    """List all view schedules in the active Revit project."""
    res = await tools.list_schedules_tool()
    return str(res)

@mcp.tool()
async def create_revit_schedule(category_name: str = "Walls", schedule_name: str = "AI Wall Schedule") -> str:
    """Create a new View Schedule for a specific category (Walls, Doors, Furniture, Plumbing Fixtures)."""
    res = await tools.create_schedule_tool(category_name, schedule_name)
    return str(res)

@mcp.tool()
async def list_revit_worksets() -> str:
    """List all user worksets in workshared BIM projects."""
    res = await tools.list_worksets_tool()
    return str(res)

@mcp.tool()
async def create_revit_workset(workset_name: str) -> str:
    """Create a new user workset in workshared BIM projects."""
    res = await tools.create_workset_tool(workset_name)
    return str(res)

@mcp.tool()
async def get_revit_element_parameters(element_id: int) -> str:
    """Inspect parameters of a specific element in Revit."""
    res = await tools.get_element_parameters_tool(element_id)
    return str(res)

@mcp.tool()
async def set_revit_element_parameter(element_id: int, parameter_name: str, parameter_value: str) -> str:
    """Set instance or type parameter on an element in Revit."""
    res = await tools.set_element_parameter_tool(element_id, parameter_name, parameter_value)
    return str(res)

if __name__ == "__main__":
    mcp.run()
