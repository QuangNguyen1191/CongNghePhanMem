"""Sinh web-ban-hang/public/images/uniqlo/manifest.json tu uniqlo_catalog.json.

CSDL chi luu 1 anh/SKU (SAN_PHAM.hinh_anh = anh dau tien cua mau sac). Web ban hang doc
manifest nay de hien them cac anh khac cua cung mau sac + anh chung cua san pham (khong doi schema).
Anh trung noi dung (site UNIQLO tra ve 1 anh duoi 2 ten) chi giu lan xuat hien dau tien.
Chay:  python3 gen_manifest_anh.py [thu_muc_public]
"""
import hashlib
import json
import os
import sys

GOC = os.path.dirname(os.path.abspath(__file__))
PUBLIC = sys.argv[1] if len(sys.argv) > 1 else os.path.join(GOC, "..", "..", "web-ban-hang", "public")
catalog = json.load(open(os.path.join(GOC, "uniqlo_catalog.json"), encoding="utf-8"))


def van_tay(url):
    f = os.path.join(PUBLIC, *url.strip("/").split("/"))
    if not os.path.exists(f):
        return url
    with open(f, "rb") as fh:
        return hashlib.md5(fh.read()).hexdigest()


manifest, bo = {}, 0
for m in catalog:
    da_co = set()

    def loc(ds):
        global bo
        kq = []
        for u in ds:
            k = van_tay(u)
            if k in da_co:
                bo += 1
                continue
            da_co.add(k)
            kq.append(u)
        return kq

    mau = {c["maMau"]: loc(c["anh"]) for c in m["mauSacs"]}
    manifest[m["uniqloId"]] = {"mau": mau, "chung": loc(m["anhChung"])}

dich = os.path.join(PUBLIC, "images", "uniqlo", "manifest.json")
os.makedirs(os.path.dirname(dich), exist_ok=True)
with open(dich, "w", encoding="utf-8", newline="\n") as f:
    json.dump(manifest, f, ensure_ascii=False, separators=(",", ":"))
tong = sum(len(a) for m in manifest.values() for a in m["mau"].values()) + sum(len(m["chung"]) for m in manifest.values())
print(f"Da ghi {dich}: {len(manifest)} san pham, {tong} anh (bo {bo} anh trung)")
