import os, sys, json, uuid, math
sys.stdout.reconfigure(encoding='utf-8')
from PIL import Image

ROOT     = r"C:\Users\rvazq\Documents\Mac\AjoloteStudios\ArtUnbound"
new_dir  = os.path.join(ROOT, r"Assets\ArtUnbound\Artworks\new")
thumb_dir= os.path.join(ROOT, r"Assets\ArtUnbound\Artworks\Thumbnails")
data_dir = os.path.join(ROOT, r"Assets\ArtUnbound\Data\Artworks")

with open(os.path.join(ROOT, r"scripts\artunbound_missing.json"), encoding="utf-8") as f:
    missing_data = json.load(f)
by_title = {a["title"]: a for a in missing_data}

stem_to_title = {
    "Lamentation of Christ, giotto": "Lamentation of Christ",
    "where do we come from":         "Where Do We Come From? What Are We? Where Are We Going?",
}

BOARD_WIDTH, BOARD_MAX_H = 0.50, 0.90
TARGETS = [64, 121, 196, 289]

def piece_count(ar, target):
    rows = max(2, round(math.sqrt(target / ar)))
    cols = max(2, round(rows * ar))
    return cols * rows

def new_guid():
    return uuid.uuid4().hex

def safe_filename(title):
    # Replace Windows-invalid chars while keeping the asset m_Name as the full title
    for ch in r'\/:*?"<>|':
        title = title.replace(ch, "")
    return title.strip()

THUMB_META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 1
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 0
    wrapV: 0
    wrapW: 0
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 100
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 0
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Android
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""

ASSET_TMPL = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 32110b9219b62be46b10a97c773a56f7, type: 3}}
  m_Name: {title}
  m_EditorClassIdentifier: Assembly-CSharp::ArtUnbound.Data.ArtworkDefinition
  artworkId: {title}
  title: {title}
  author: {author}
  year: {year}
  description: "Museum: {museum}\\nArt Movement: {artMovement}\\nYear: {year}"
  museum: {museum}
  artMovement: {artMovement}
  aspectRatio: {ar:.4f}
  baseSizePortrait: {{x: 0.5, y: 0.7}}
  baseSizeLandscape: {{x: 0.7, y: 0.5}}
  thumbnail: {{fileID: 21300000, guid: {thumb_guid}, type: 3}}
  fullImage: {{fileID: 21300000, guid: {full_guid}, type: 3}}
  previewTexture: {{fileID: 0}}
  puzzleTexture: {{fileID: 0}}
  isBaseContent: 1
  requiresUnlock: 0
  unlockWeek: 0
  pieceCountEasy: {easy}
  pieceCountNormal: {normal}
  pieceCountHard: {hard}
  pieceCountExpert: {expert}
  complexity: 1
  colorVariety: 3
  detailLevel: 3
"""

ASSET_META = """fileFormatVersion: 2
guid: {guid}
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData:
  assetBundleName:
  assetBundleVariant:
"""

errors = []
processed = 0

for fname in sorted(os.listdir(new_dir)):
    if not fname.lower().endswith((".jpg", ".jpeg", ".png")) or fname.endswith(".meta"):
        continue

    stem = os.path.splitext(fname)[0]
    title = stem_to_title.get(stem, stem)

    if title not in by_title:
        errors.append(f"[SKIP] No metadata for: {fname!r} -> {title!r}")
        continue

    meta = by_title[title]

    # Read source GUID
    src_meta = os.path.join(new_dir, fname + ".meta")
    full_guid = None
    with open(src_meta, encoding="utf-8") as mf:
        for line in mf:
            if line.strip().startswith("guid:"):
                full_guid = line.strip().split(":", 1)[1].strip()
                break
    if not full_guid:
        errors.append(f"[SKIP] No GUID in {src_meta}")
        continue

    # Image dimensions and aspect ratio
    with Image.open(os.path.join(new_dir, fname)) as img:
        w, h = img.size
    ar = w / h

    easy, normal, hard, expert = [piece_count(ar, t) for t in TARGETS]

    # Thumbnail .meta
    thumb_fname    = stem + ".jpg"
    thumb_meta_path = os.path.join(thumb_dir, thumb_fname + ".meta")
    if os.path.exists(thumb_meta_path):
        with open(thumb_meta_path, encoding="utf-8") as tf:
            thumb_guid = None
            for line in tf:
                if line.strip().startswith("guid:"):
                    thumb_guid = line.strip().split(":", 1)[1].strip()
                    break
    else:
        thumb_guid = new_guid()
        with open(thumb_meta_path, "w", encoding="utf-8", newline="\n") as tf:
            tf.write(THUMB_META.replace("{guid}", thumb_guid))

    # .asset file
    asset_path = os.path.join(data_dir, safe_filename(title) + ".asset")
    with open(asset_path, "w", encoding="utf-8", newline="\n") as af:
        af.write(ASSET_TMPL.format(
            title=title, author=meta["author"], year=meta["year"],
            museum=meta["museum"], artMovement=meta["artMovement"],
            ar=ar, thumb_guid=thumb_guid, full_guid=full_guid,
            easy=easy, normal=normal, hard=hard, expert=expert,
        ))

    # .asset.meta file
    asset_meta_path = asset_path + ".meta"
    if not os.path.exists(asset_meta_path):
        with open(asset_meta_path, "w", encoding="utf-8", newline="\n") as amf:
            amf.write(ASSET_META.replace("{guid}", new_guid()))

    print(f"[OK] {title}  {w}x{h}  ar={ar:.3f}  pieces={easy}/{normal}/{hard}/{expert}")
    processed += 1

print()
print(f"Created: {processed}  |  Errors: {len(errors)}")
for e in errors:
    print(f"  {e}")
