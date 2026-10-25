"""Basic publication checks; reports paths only, never detected values."""
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]
problems = []
skip = {'.git', '.vs', 'bin', 'obj', '__pycache__'}
for p in root.rglob('*'):
    if not p.is_file():
        continue
    rel = p.relative_to(root)
    if any(part.lower() in skip for part in rel.parts[:-1]):
        continue
    if any(part.lower() in {'.minecraft', 'pcl', 'logs'} for part in rel.parts[:-1]):
        problems.append(f'Runtime data: {rel.as_posix()}')
    if p.suffix.lower() in {'.exe', '.pdb', '.log', '.ini', '.pfx', '.p12', '.pem', '.key', '.snk', '.user', '.suo'} or p.name.startswith('.env'):
        problems.append(f'Private/output file: {rel.as_posix()}')
    try:
        text = p.read_text(encoding='utf-8-sig')
    except (UnicodeError, OSError):
        continue
    patterns = [
        r'gh[pousr]_[A-Za-z0-9]{20,}',
        r'github_pat_[A-Za-z0-9_]{20,}',
        r'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----',
        r'eyJ[A-Za-z0-9_-]{15,}\.[A-Za-z0-9_-]{15,}\.[A-Za-z0-9_-]{10,}',
        r'\$2[aby]\$\d{2}\$[A-Za-z0-9./]{40,}',
        r'(?i)[A-Z]:[\\/]Users[\\/](?!Public[\\/]|Default[\\/])[A-Za-z0-9_.-]+[\\/]',
    ]
    if any(re.search(pattern, text) for pattern in patterns):
        problems.append(f'Potential credential or personal path: {rel.as_posix()}')
secret = (root / 'Plain Craft Launcher 2/Modules/ModSecret.vb').read_text(encoding='utf-8-sig')
for field, env in [('OAuthClientId', 'PCL_OAUTH_CLIENT_ID'), ('CurseForgeAPIKey', 'PCL_CURSEFORGE_API_KEY')]:
    expected = f'Public {field} As String = If(Environment.GetEnvironmentVariable("{env}"), "")'
    if expected not in secret:
        problems.append(f'Unexpected credential configuration: {field}')
if not re.search(r'Public Const ClsBaseUrl As String = ""', secret):
    problems.append('Telemetry endpoint is not empty')
if problems:
    print('\n'.join(problems))
    sys.exit(1)
print('Public-source check passed: no configured credentials or common private/output files detected.')
