from revit_client import revit_client
from typing import Dict, Any, Optional, List

async def ping_revit() -> Dict[str, Any]:
    """Ping the running Autodesk Revit instance to check add-in connectivity."""
    return await revit_client.send_command("ping")

async def say_hello_tool() -> Dict[str, Any]:
    """Display a greeting dialog in Revit (connection test)."""
    return await revit_client.send_command("say_hello")

async def create_sheets_from_midp_list_tool(sheets: Optional[List[Dict[str, str]]] = None) -> Dict[str, Any]:
    """Create drawing sheets in Revit from a Master Information Delivery Plan (MIDP) list."""
    payload = {"sheets": sheets or []}
    return await revit_client.send_command("create_sheets_from_midp_list", payload)

async def create_midp_sheets_tool() -> Dict[str, Any]:
    """Batch create 20 standard electrical layout & elevation drawing sheets from the preset MIDP master drawing list."""
    return await revit_client.send_command("create_midp_sheets")

async def create_lean_to_roof_tool(overhang_mm: float = 500.0, slope_degrees: float = 10.0, level_name: str = "Level 1", roof_type_name: Optional[str] = None) -> Dict[str, Any]:
    """Create a mono-pitch / lean-to roof with overhang (in mm) and slope (in degrees) over the room/walls top."""
    payload = {"overhang_mm": overhang_mm, "slope_degrees": slope_degrees, "level_name": level_name, "roof_type_name": roof_type_name}
    return await revit_client.send_command("create_lean_to_roof", payload)

async def paint_exterior_walls_tool(r: int = 255, g: int = 0, b: int = 0) -> Dict[str, Any]:
    """Paint/Override graphic color of all external walls in the active view (e.g. Red)."""
    return await revit_client.send_command("paint_exterior_walls", {"r": r, "g": g, "b": b})

async def highlight_walls_by_thickness_tool(thickness_mm: float = 200.0, r: int = 255, g: int = 255, b: int = 0) -> Dict[str, Any]:
    """Highlight walls of a specific thickness (e.g. 200mm) in Yellow or custom RGB color."""
    return await revit_client.send_command("highlight_walls_by_thickness", {"thickness_mm": thickness_mm, "r": r, "g": g, "b": b})

async def override_graphics_in_view_tool(category_name: str = "Walls", r: int = 255, g: int = 0, b: int = 0) -> Dict[str, Any]:
    """Override graphics/color of elements by category in active shaded view."""
    return await revit_client.send_command("override_graphics_in_view", {"category_name": category_name, "r": r, "g": g, "b": b})

async def get_current_view_info_tool() -> Dict[str, Any]:
    """Get current active view info (name, view type, scale, level)."""
    return await revit_client.send_command("get_current_view_info")

async def get_current_view_elements_tool() -> Dict[str, Any]:
    """Get elements visible from the current active view."""
    return await revit_client.send_command("get_current_view_elements")

async def get_available_family_types_tool(category_name: Optional[str] = None) -> Dict[str, Any]:
    """Get available family types loaded in current project."""
    return await revit_client.send_command("get_available_family_types", {"category_name": category_name})

async def get_selected_elements_tool() -> Dict[str, Any]:
    """Get currently selected elements in the Revit user interface."""
    return await revit_client.send_command("get_selected_elements")

async def get_material_quantities_tool() -> Dict[str, Any]:
    """Calculate material quantities and takeoffs in project."""
    return await revit_client.send_command("get_material_quantities")

async def ai_element_filter_tool(category_name: str = "Generic Models", level_name: Optional[str] = None) -> Dict[str, Any]:
    """Intelligent element querying tool for AI assistants."""
    return await revit_client.send_command("ai_element_filter", {"category_name": category_name, "level_name": level_name})

async def analyze_model_statistics_tool() -> Dict[str, Any]:
    """Analyze model complexity with element counts per category."""
    return await revit_client.send_command("analyze_model_statistics")

async def create_point_based_element_tool(family_type_name: str, x: float = 0.0, y: float = 0.0, z: float = 0.0, level_name: str = "Level 1") -> Dict[str, Any]:
    """Create point-based elements (door, window, furniture, lighting)."""
    payload = {"family_type_name": family_type_name, "x": x, "y": y, "z": z, "level_name": level_name}
    return await revit_client.send_command("create_point_based_element", payload)

async def create_line_based_element_tool(category_name: str = "Wall", start_x: float = 0.0, start_y: float = 0.0, end_x: float = 10.0, end_y: float = 0.0, level_name: str = "Level 1") -> Dict[str, Any]:
    """Create line-based elements (wall, beam, pipe, duct)."""
    payload = {"category_name": category_name, "start_x": start_x, "start_y": start_y, "end_x": end_x, "end_y": end_y, "level_name": level_name}
    return await revit_client.send_command("create_line_based_element", payload)

async def create_surface_based_element_tool(category_name: str = "Floor", level_name: str = "Level 1") -> Dict[str, Any]:
    """Create surface-based elements (floor, ceiling, roof)."""
    if category_name and category_name.lower() in ["roof", "roofs"]:
        return await create_lean_to_roof_tool(overhang_mm=500.0, slope_degrees=10.0, level_name=level_name)
    payload = {"category": category_name, "level": level_name}
    return await revit_client.send_command("create_element", payload)

async def create_grid_tool(x1: float = 0.0, y1: float = 0.0, x2: float = 10.0, y2: float = 0.0, name: str = "1") -> Dict[str, Any]:
    """Create a grid line with specified coordinates and name."""
    payload = {"x1": x1, "y1": y1, "x2": x2, "y2": y2, "name": name}
    return await revit_client.send_command("create_grid", payload)

async def create_level_tool(elevation_meters: float = 4.0, level_name: str = "Level 2") -> Dict[str, Any]:
    """Create levels at specified elevations in meters."""
    payload = {"elevation_meters": elevation_meters, "level_name": level_name}
    return await revit_client.send_command("create_level", payload)

async def create_room_tool(name: str = "Room 101", number: str = "101", level_name: str = "Level 1") -> Dict[str, Any]:
    """Create and place rooms at specified locations."""
    payload = {"category": "Room", "level": level_name, "parameters": {"name": name, "number": number}}
    return await revit_client.send_command("create_element", payload)

async def create_dimensions_tool() -> Dict[str, Any]:
    """Create dimension annotations in the current view."""
    return await revit_client.send_command("tag_all_walls")

async def create_structural_framing_system_tool(start_x: float = 0.0, start_y: float = 0.0, end_x: float = 10.0, end_y: float = 0.0, level_name: str = "Level 1") -> Dict[str, Any]:
    """Create a structural beam framing system."""
    payload = {"start_x": start_x, "start_y": start_y, "end_x": end_x, "end_y": end_y, "level_name": level_name, "is_structural": True}
    return await revit_client.send_command("create_line_based_element", payload)

async def delete_element_tool(element_id: int) -> Dict[str, Any]:
    """Delete elements by ID."""
    return await revit_client.send_command("delete_element", {"element_id": element_id})

async def operate_element_tool(element_id: int, operation: str = "select") -> Dict[str, Any]:
    """Operate on elements (select, hide)."""
    return await revit_client.send_command("operate_element", {"element_id": element_id, "operation": operation})

async def color_elements_tool(element_id: int, r: int = 255, g: int = 0, b: int = 0) -> Dict[str, Any]:
    """Color elements based on RGB values."""
    return await revit_client.send_command("color_elements", {"element_id": element_id, "r": r, "g": g, "b": b})

async def tag_all_walls_tool() -> Dict[str, Any]:
    """Tag all walls in the current active view."""
    return await revit_client.send_command("tag_all_walls")

async def tag_all_rooms_tool() -> Dict[str, Any]:
    """Tag all rooms in the current active view."""
    return await revit_client.send_command("tag_all_rooms")

async def create_sheet_tool(sheet_number: str = "A101", sheet_name: str = "AI AUTOMATED SHEET") -> Dict[str, Any]:
    """Create a drawing sheet in Revit."""
    return await revit_client.send_command("create_sheet", {"sheet_number": sheet_number, "sheet_name": sheet_name})

async def create_sheets_for_levels_tool() -> Dict[str, Any]:
    """Automatically create drawing sheets for all levels in model (Level 1, Level 2, etc.) and place floor plan views on them."""
    return await revit_client.send_command("create_sheets_for_levels")

async def export_room_data_tool() -> Dict[str, Any]:
    """Export all room data from the project as JSON."""
    return await revit_client.send_command("export_room_data")

async def store_project_data_tool() -> Dict[str, Any]:
    """Store project metadata in local JSON file."""
    return await revit_client.send_command("store_project_data")

async def store_room_data_tool() -> Dict[str, Any]:
    """Store room metadata in local JSON file."""
    return await revit_client.send_command("store_room_data")

async def query_stored_data_tool() -> Dict[str, Any]:
    """Query stored project and room data from local JSON storage."""
    return await revit_client.send_command("query_stored_data")

async def send_code_to_revit_tool(code: str) -> Dict[str, Any]:
    """Execute dynamic C# code using Roslyn CSharpScript engine in Revit."""
    return await revit_client.send_command("send_code_to_revit", {"code": code})
