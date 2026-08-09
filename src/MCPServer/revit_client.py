import httpx
from typing import Dict, Any, Optional

class RevitClient:
    """HTTP Client for communicating with the embedded Revit C# Add-In REST server."""

    def __init__(self, base_url: str = "http://localhost:8080/revit/v1/"):
        self.base_url = base_url

    async def send_command(self, action: str, payload: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        if payload is None:
            payload = {}
        
        request_body = {
            "action": action,
            "payload": payload
        }

        async with httpx.AsyncClient(timeout=30.0) as client:
            try:
                response = await client.post(self.base_url, json=request_body)
                response.raise_for_status()
                return response.json()
            except httpx.HTTPError as exc:
                return {
                    "status": "error",
                    "message": f"Failed to connect to Revit Add-In: {str(exc)}"
                }

revit_client = RevitClient()
