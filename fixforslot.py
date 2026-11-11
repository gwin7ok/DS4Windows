import re,subprocess,sys,collections
out=subprocess.run(['dotnet','build','DS4WindowsTests','-c','Debug','-p:platform=x64'],capture_output=True,text=True,encoding='utf-8',errors='replace').stdout
errs=set(re.findall(r"^(G:[^()]+)\((\d+),\d+\): error CS1061: 'BackingStore' に 'ForSlot'",out,re.M))
by=collections.defaultdict(set)
for f,l in errs: by[f].add(int(l))
areaMap={'Sticks':'stickSlots','Touchpad':'touchpadSlots','Triggers':'triggerSlots','Gyro':'gyroSlots','Lightbar':'lightbarSlots','Output':'outputSlots','General':'generalSlots','SixAxis':'sixAxisSlots'}
for f,ls in by.items():
    raw=open(f,'rb').read(); bom=raw.startswith(b'\xef\xbb\xbf'); s=raw.decode('utf-8-sig'); crlf='\r\n' in s; lines=s.replace('\r\n','\n').split('\n')
    for l in ls:
        t=lines[l-1]
        t=re.sub(r'(\w+)\.ForSlot\(([^()]+)\)\.(\w+)\.',lambda m:'%s.%s[%s].'%(m.group(1),areaMap[m.group(3)],m.group(2)),t)
        lines[l-1]=t
    s='\n'.join(lines)
    if crlf:s=s.replace('\n','\r\n')
    open(f,'wb').write((b'\xef\xbb\xbf' if bom else b'')+s.encode('utf-8'))
print(len(errs),'fixed')
rest=[l for l in out.split('\n') if ' error ' in l and "'BackingStore' に 'ForSlot'" not in l]
print('\n'.join(sorted(set(re.sub(r'\[G:.*','',x)[:220] for x in rest)))[:3000])
