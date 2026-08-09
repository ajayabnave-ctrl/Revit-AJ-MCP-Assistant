from revit_client import revit_client
from typing import Dict, Any

async def ping_revit() -> Dict[str, Any]:
    """Ping the running Autodesk Revit instance to check add-in connectivity."""
    return await revit_client.send_command("ping")

async def get_active_document_info() -> Dict[str, Any]:
    """Retrieve metadata about the active Autodesk Revit BIM document."""
    return await revit_client.send_command("get_document_info")

async def create_wall_tool(start_x: float = 0.0, start_y: float = 0.0, end_x: float = 20.0, end_y: float = 0.0, level_name: str = "Level 1") -> Dict[str, Any]:
    """Create a wall element in Autodesk Revit given start/end coordinates and level name."""
    payload = {
        "start_x": start_x,
        "start_y": start_y,
        "end_x": end_x,
        "end_y": end_y,
        "level": level_name
    }
    return await revit_client.send_command("create_wall", payload)

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
