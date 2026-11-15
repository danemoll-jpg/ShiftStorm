import json,struct,numpy as np,sys
def load(p):
  b=open(p,'rb').read(); l=struct.unpack('<I',b[12:16])[0]; j=json.loads(b[20:20+l])
  bin_off=20+l+8
  def acc(i):
    a=j['accessors'][i]; bv=j['bufferViews'][a['bufferView']]
    n={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4}[a['type']]
    dt={5126:np.float32,5125:np.uint32,5123:np.uint16,5121:np.uint8}[a['componentType']]
    off=bin_off+bv.get('byteOffset',0)+a.get('byteOffset',0)
    return np.frombuffer(b,dtype=dt,count=a['count']*n,offset=off).reshape(a['count'],n) if n>1 else np.frombuffer(b,dtype=dt,count=a['count'],offset=off)
  p=j['meshes'][0]['primitives'][0]
  return j,acc(p['attributes']['POSITION']),acc(p['indices']), acc(p['attributes']['TEXCOORD_0'])
