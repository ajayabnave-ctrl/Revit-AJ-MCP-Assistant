from revit_client import revit_client
from typing import Dict, Any, Optional, List

async def ping_revit() -> Dict[str, Any]:
    """Ping the running Autodesk Revit instance to check add-in connectivity."""
    return await revit_client.send_command("ping")

async def get_active_document_info() -> Dict[str, Any]:
    """Retrieve metadata about the active Autodesk Revit BIM document."""
    return await revit_client.send_command("get_document_info")

async def create_wall_tool(start_x: float = 0.0, start_y: float = 0.0, end_x: float = 20.0, end_y: float = 0.0, level_name: str = "Level 1") -> Dict[str, Any]:
    """Create a standard wall element in Autodesk Revit."""
    payload = {
        "start_x": start_x,
        "start_y": start_y,
        "end_x": end_x,
        "end_y": end_y,
        "level": level_name
    }
    return await revit_client.send_command("create_wall", payload)

async def create_wall_advanced_tool(
    start_x: float = 0.0, 
    start_y: float = 0.0, 
    end_x: float = 20.0, 
    end_y: float = 0.0, 
    level_name: str = "Level 1",
    height_feet: float = 10.0,
    top_level_name: Optional[str] = None,
    wall_type_name: Optional[str] = None,
    is_structural: bool = False
) -> Dict[str, Any]:
    """Create a wall with full control over height, top constraint, wall type, and structural flag."""
    payload = {
        "start_x": start_x,
        "start_y": start_y,
        "end_x": end_x,
        "end_y": end_y,
        "level_name": level_name,
        "height_feet": height_feet,
        "top_level_name": top_level_name,
        "wall_type_name": wall_type_name,
        "is_structural": is_structural
    }
    return await revit_client.send_command("create_wall_advanced", payload)

async def query_elements_tool(category_name: str = "Plumbing Fixtures", level_name: Optional[str] = None) -> Dict[str, Any]:
    """Query elements/fixtures in Revit by category (Plumbing Fixtures, Furniture, Doors, Windows, Mechanical Equipment, Lighting Fixtures)."""
    payload = {
        "category_name": category_name,
        "level_name": level_name
    }
    return await revit_client.send_command("query_elements", payload)

async def list_sheets_tool() -> Dict[str, Any]:
    """Retrieve list of all drawing sheets in the active Revit model."""
    return await revit_client.send_command("list_sheets")

async def create_sheet_tool(sheet_number: str = "A101", sheet_name: str = "AI AUTOMATED SHEET") -> Dict[str, Any]:
    """Create a new sheet with title block in the active Revit model."""
    payload = {
        "sheet_number": sheet_number,
        "sheet_name": sheet_name
    }
    return await revit_client.send_command("create_sheet", payload)

async def list_schedules_tool() -> Dict[str, Any]:
    """Retrieve list of all view schedules in the active Revit project."""
    return await revit_client.send_command("list_schedules")

async def create_schedule_tool(category_name: str = "Walls", schedule_name: str = "AI Schedule") -> Dict[str, Any]:
    """Create a new View Schedule for a specific category (Walls, Doors, Furniture, Plumbing Fixtures, Lighting Fixtures)."""
    payload = {
        "category_name": category_name,
        "schedule_name": schedule_name
    }
    return await revit_client.send_command("create_schedule", payload)

async def create_lighting_schedule_tool(schedule_name: str = "Lighting Fixture Schedule") -> Dict[str, Any]:
    """Create a specialized Lighting Fixture Schedule with fields (Family and Type, Level, Count, Circuit Number, Panel, Comments)."""
    payload = {
        "category_name": "Lighting Fixtures",
        "schedule_name": schedule_name
    }
    return await revit_client.send_command("create_lighting_schedule", payload)

async def create_schedule_advanced_tool(
    category_name: str = "Lighting Fixtures",
    schedule_name: str = "Lighting Fixture Schedule",
    fields: Optional[List[str]] = None,
    sort_by: str = "Level",
    itemize_instances: bool = True
) -> Dict[str, Any]:
    """Create a custom schedule for any category with custom fields, sorting, and instance itemization."""
    payload = {
        "category_name": category_name,
        "schedule_name": schedule_name,
        "fields": fields or ["Family and Type", "Level", "Count", "Circuit Number", "Panel"],
        "sort_by": sort_by,
        "itemize_instances": itemize_instances
    }
    return await revit_client.send_command("create_schedule_advanced", payload)

async def list_worksets_tool() -> Dict[str, Any]:
    """List all user worksets in workshared BIM projects."""
    return await revit_client.send_command("list_worksets")

async def create_workset_tool(workset_name: str) -> Dict[str, Any]:
    """Create a new user workset in workshared BIM projects."""
    payload = {
        "workset_name": workset_name
    }
    return await revit_client.send_command("create_workset", payload)

async def get_element_parameters_tool(element_id: int) -> Dict[str, Any]:
    """Inspect parameters of a specific element in Revit."""
    payload = {
        "element_id": element_id
    }
    return await revit_client.send_command("get_element_parameters", payload)

async def set_element_parameter_tool(element_id: int, parameter_name: str, parameter_value: str) -> Dict[str, Any]:
    """Set instance or type parameter on an element in Revit."""
    payload = {
        "element_id": element_id,
        "parameter_name": parameter_name,
        "parameter_value": parameter_value
    }
    return await revit_client.send_command("set_element_parameter", payload)
