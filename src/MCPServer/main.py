import asyncio
import sys
from typing import Optional, List, Dict, Any

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
async def say_hello() -> str:
    """Display a greeting dialog in Revit (connection test)."""
    res = await tools.say_hello_tool()
    return str(res)

@mcp.tool()
async def get_current_view_info() -> str:
    """Get current active view info (name, view type, scale, level)."""
    res = await tools.get_current_view_info_tool()
    return str(res)

@mcp.tool()
async def get_current_view_elements() -> str:
    """Get elements visible in the current active view."""
    res = await tools.get_current_view_elements_tool()
    return str(res)

@mcp.tool()
async def get_available_family_types(category_name: Optional[str] = None) -> str:
    """Get available family types in current project."""
    res = await tools.get_available_family_types_tool(category_name)
    return str(res)

@mcp.tool()
async def get_selected_elements() -> str:
    """Get currently selected elements in the Revit user interface."""
    res = await tools.get_selected_elements_tool()
    return str(res)

@mcp.tool()
async def get_material_quantities() -> str:
    """Calculate material quantities and takeoffs in project."""
    res = await tools.get_material_quantities_tool()
    return str(res)

@mcp.tool()
async def ai_element_filter(category_name: str = "Generic Models", level_name: Optional[str] = None) -> str:
    """Intelligent element querying tool for AI assistants."""
    res = await tools.ai_element_filter_tool(category_name, level_name)
    return str(res)

@mcp.tool()
async def analyze_model_statistics() -> str:
    """Analyze model complexity with element counts per category."""
    res = await tools.analyze_model_statistics_tool()
    return str(res)

@mcp.tool()
async def create_point_based_element(family_type_name: str, x: float = 0.0, y: float = 0.0, z: float = 0.0, level_name: str = "Level 1") -> str:
    """Create point-based elements (door, window, furniture, lighting)."""
    res = await tools.create_point_based_element_tool(family_type_name, x, y, z, level_name)
    return str(res)

@mcp.tool()
async def create_line_based_element(category_name: str = "Wall", start_x: float = 0.0, start_y: float = 0.0, end_x: float = 10.0, end_y: float = 0.0, level_name: str = "Level 1") -> str:
    """Create line-based elements (wall, beam, pipe, duct)."""
    res = await tools.create_line_based_element_tool(category_name, start_x, start_y, end_x, end_y, level_name)
    return str(res)

@mcp.tool()
async def create_surface_based_element(category_name: str = "Floor", level_name: str = "Level 1") -> str:
    """Create surface-based elements (floor, ceiling, roof)."""
    res = await tools.create_surface_based_element_tool(category_name, level_name)
    return str(res)

@mcp.tool()
async def create_grid(x1: float = 0.0, y1: float = 0.0, x2: float = 10.0, y2: float = 0.0, name: str = "1") -> str:
    """Create a grid system with smart spacing generation."""
    res = await tools.create_grid_tool(x1, y1, x2, y2, name)
    return str(res)

@mcp.tool()
async def create_level(elevation_meters: float = 4.0, level_name: str = "Level 2") -> str:
    """Create levels at specified elevations in meters."""
    res = await tools.create_level_tool(elevation_meters, level_name)
    return str(res)

@mcp.tool()
async def create_room(name: str = "Room 101", number: str = "101", level_name: str = "Level 1") -> str:
    """Create and place rooms at specified locations."""
    res = await tools.create_room_tool(name, number, level_name)
    return str(res)

@mcp.tool()
async def create_sheet(sheet_number: str = "A101", sheet_name: str = "AI AUTOMATED SHEET") -> str:
    """Create a new drawing sheet in the active Revit model."""
    res = await tools.create_sheet_tool(sheet_number, sheet_name)
    return str(res)

@mcp.tool()
async def create_sheets_for_levels() -> str:
    """Automatically create drawing sheets for all levels in model (Level 1, Level 2, etc.) and place floor plan views on them."""
    res = await tools.create_sheets_for_levels_tool()
    return str(res)

@mcp.tool()
async def create_dimensions() -> str:
    """Create dimension annotations in the current view."""
    res = await tools.create_dimensions_tool()
    return str(res)

@mcp.tool()
async def create_structural_framing_system(start_x: float = 0.0, start_y: float = 0.0, end_x: float = 10.0, end_y: float = 0.0, level_name: str = "Level 1") -> str:
    """Create a structural beam framing system."""
    res = await tools.create_structural_framing_system_tool(start_x, start_y, end_x, end_y, level_name)
    return str(res)

@mcp.tool()
async def delete_element(element_id: int) -> str:
    """Delete elements by ID."""
    res = await tools.delete_element_tool(element_id)
    return str(res)

@mcp.tool()
async def operate_element(element_id: int, operation: str = "select") -> str:
    """Operate on elements (select, hide)."""
    res = await tools.operate_element_tool(element_id, operation)
    return str(res)

@mcp.tool()
async def color_elements(element_id: int, r: int = 255, g: int = 0, b: int = 0) -> str:
    """Color elements based on RGB values."""
    res = await tools.color_elements_tool(element_id, r, g, b)
    return str(res)

@mcp.tool()
async def tag_all_walls() -> str:
    """Tag all walls in the current view."""
    res = await tools.tag_all_walls_tool()
    return str(res)

@mcp.tool()
async def tag_all_rooms() -> str:
    """Tag all rooms in the current view."""
    res = await tools.tag_all_rooms_tool()
    return str(res)

@mcp.tool()
async def export_room_data() -> str:
    """Export all room data from the project."""
    res = await tools.export_room_data_tool()
    return str(res)

@mcp.tool()
async def store_project_data() -> str:
    """Store project metadata in local JSON storage."""
    res = await tools.store_project_data_tool()
    return str(res)

@mcp.tool()
async def store_room_data() -> str:
    """Store room metadata in local JSON storage."""
    res = await tools.store_room_data_tool()
    return str(res)

@mcp.tool()
async def query_stored_data() -> str:
    """Query stored project and room data."""
    res = await tools.query_stored_data_tool()
    return str(res)

@mcp.tool()
async def send_code_to_revit(code: str) -> str:
    """Execute dynamic C# code using Roslyn CSharpScript engine in Revit."""
    res = await tools.send_code_to_revit_tool(code)
    return str(res)

@mcp.tool()
async def ping_revit_status() -> str:
    """Check connection status with Autodesk Revit."""
    res = await tools.ping_revit()
    return str(res)

if __name__ == "__main__":
    mcp.run()
