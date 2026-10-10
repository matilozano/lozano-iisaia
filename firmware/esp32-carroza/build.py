"""Cross-compile portable MicroPython bytecode; no board or C toolchain needed."""
from pathlib import Path
import subprocess
import sys

root = Path(__file__).parent.resolve()
sys.path.insert(0, str(root / ".tools"))
import mpy_cross

output = root / "build"
output.mkdir(exist_ok=True)
for source in sorted((root / "src").glob("*.py")):
    if source.name == "config_local.py":
        continue  # Never package credentials in build artifacts.
    subprocess.run([mpy_cross.mpy_cross, "-o", str(output / (source.stem + ".mpy")), str(source)], check=True)
print("OK: MicroPython bytecode compiled", output)
