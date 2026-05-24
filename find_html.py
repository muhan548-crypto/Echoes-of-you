import json
import os

fpath = r"C:\Users\lol xdd\.gemini\antigravity\brain\b3f1c03f-c64f-44ad-b1c4-044f5d1d9d96\.system_generated\logs\transcript.jsonl"
print(f"Checking if log exists: {os.path.exists(fpath)}")
if os.path.exists(fpath):
    with open(fpath, 'r', encoding='utf-8') as f:
        for idx, line in enumerate(f):
            if '"type":"USER_INPUT"' in line and 'System Calibration - Echoes' in line:
                print(f"Found line {idx}")
                data = json.loads(line)
                content = data.get('content', '')
                with open('scratch_request5.txt', 'w', encoding='utf-8') as out:
                    out.write(content)
                print("Extracted content to scratch_request5.txt successfully!")
                break
else:
    print("Log file not found.")
