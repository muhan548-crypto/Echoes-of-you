import json
import os

log_path = r"C:\Users\lol xdd\.gemini\antigravity\brain\b3f1c03f-c64f-44ad-b1c4-044f5d1d9d96\.system_generated\logs\transcript.jsonl"
print(f"Log path exists: {os.path.exists(log_path)}")

if os.path.exists(log_path):
    with open(log_path, 'r', encoding='utf-8') as f:
        for idx, line in enumerate(f):
            if '"type":"USER_INPUT"' in line:
                try:
                    data = json.loads(line)
                    content = data.get('content', '')
                    if 'System Calibration' in content:
                        fname = f"extracted_settings_{idx}.html"
                        with open(fname, 'w', encoding='utf-8') as out:
                            out.write(content)
                        print(f"Extracted settings menu to {fname}")
                    elif 'lang="en"' in content or 'lang=\\"en\\"' in content:
                        fname = f"extracted_menu_{idx}.html"
                        with open(fname, 'w', encoding='utf-8') as out:
                            out.write(content)
                        print(f"Extracted menu/HUD html to {fname}")
                except Exception as e:
                    print(f"Error parsing line {idx}: {e}")
