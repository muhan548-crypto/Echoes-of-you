import json
import os
import re

log_path = r"C:\Users\lol xdd\.gemini\antigravity\brain\b3f1c03f-c64f-44ad-b1c4-044f5d1d9d96\.system_generated\logs\transcript.jsonl"
print(f"Log path exists: {os.path.exists(log_path)}")

if os.path.exists(log_path):
    with open(log_path, 'r', encoding='utf-8') as f:
        for idx, line in enumerate(f):
            if 'USER_INPUT' in line or 'USER_EXPLICIT' in line:
                try:
                    data = json.loads(line)
                    content = data.get('content', '')
                    # Search for HTML
                    html_matches = re.findall(r'(<!DOCTYPE html>.*</html>)', content, re.DOTALL | re.IGNORECASE)
                    for h_idx, html in enumerate(html_matches):
                        fname = f"extracted_all_html_{idx}_{h_idx}.html"
                        with open(fname, 'w', encoding='utf-8') as out:
                            out.write(html)
                        print(f"Extracted HTML matching from line {idx} to {fname}")
                except Exception as e:
                    pass
