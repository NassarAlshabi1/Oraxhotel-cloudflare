from pathlib import Path
import json

for name in ("HotelSys/appsettings.json", "HotelSys/appsettings.Production.json.example"):
    path = Path(name)
    json.loads(path.read_text(encoding="utf-8-sig"))
    print(f"json_validation=pass {name}")
