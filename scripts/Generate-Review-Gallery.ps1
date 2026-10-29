$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$previewDirectory = Join-Path $projectRoot 'preview'
$outputPath = Join-Path $previewDirectory 'gallery.html'
$referenceRoot = Join-Path $projectRoot 'Slime_Ranching_Overhaul_Updated'

New-Item -ItemType Directory -Path $previewDirectory -Force | Out-Null

$imageFiles = @(
    Get-ChildItem -LiteralPath (Join-Path $projectRoot 'assets') -Recurse -File -ErrorAction SilentlyContinue
    Get-ChildItem -LiteralPath (Join-Path $previewDirectory 'ui') -Recurse -File -ErrorAction SilentlyContinue
    Get-ChildItem -LiteralPath (Join-Path $previewDirectory 'reviews') -File -Filter '*-comparison.png' -ErrorAction SilentlyContinue
    Get-ChildItem -LiteralPath $referenceRoot -Recurse -File -ErrorAction SilentlyContinue
) | Where-Object { $_.Extension -match '^\.(png|jpe?g|webp|gif|svg)$' }
$skippedLongPaths = @($imageFiles | Where-Object { $_.FullName.Length -ge 250 })
$imageFiles = @($imageFiles | Where-Object { $_.FullName.Length -lt 250 })

$baseUri = [Uri]($previewDirectory.TrimEnd('\') + '\')
$cards = foreach ($file in $imageFiles) {
    $relativePath = $file.FullName.Substring($projectRoot.Length).TrimStart('\').Replace('\', '/')
    if ($relativePath -match '(?i)Slime_Ranching_Overhaul-original-ignore-this') {
        continue
    }

    $name = [IO.Path]::GetFileNameWithoutExtension($file.Name)
    $categories = [Collections.Generic.List[string]]::new()
    $isActive = $relativePath.StartsWith('assets/', [StringComparison]::OrdinalIgnoreCase)
    $isCurrentUiPreview = $relativePath.StartsWith('preview/ui/', [StringComparison]::OrdinalIgnoreCase)
    $isArtworkComparison = $relativePath.StartsWith('preview/reviews/', [StringComparison]::OrdinalIgnoreCase)

    if ($isCurrentUiPreview) { $categories.Add('UI') }
    if ($relativePath -match '(?i)^assets/Buildings/') { $categories.Add('Buildings') }
    if ($relativePath -match '(?i)^assets/Machines/') { $categories.Add('Machines') }
    if ($relativePath -match '(?i)^assets/Furniture/') { $categories.Add('Furniture') }
    if ($relativePath -match '(?i)^assets/(Slimes|Animals)/') { $categories.Add('Slimes') }

    if ($relativePath -match '(?i)redesigns.*old|building|barn|daycare|interior layout') {
        $categories.Add('Buildings')
    }
    if (-not $isCurrentUiPreview -and $name -match '(?i)furnature|furniture|feeding trough|trampoline|cushion|slide|tunnel|pond|swing|bed') {
        $categories.Add('Furniture')
    }
    if (-not $isCurrentUiPreview -and $name -match '(?i)breeder|incubat|machine|crystal|mutagenic|refinery') {
        $categories.Add('Machines')
        $categories.Add('UI')
        $categories.Add('Slimes')
    }
    if (-not $isCurrentUiPreview -and $name -match '(?i)ui|interface|menu|interaction|stages') {
        $categories.Add('UI')
    }
    if (-not $isActive -and -not $isCurrentUiPreview -and $name -match '(?i)slime|furnature|furniture|feeding trough|machines') {
        $categories.Add('Slimes')
    }

    $categories = @($categories | Sort-Object -Unique)
    if ($categories.Count -eq 0) {
        $categories = @('Reference')
    }

    $status = if ($relativePath -match '(?i)superseded-custom-map') { 'Superseded map experiment' }
        elseif ($isCurrentUiPreview) { 'Current UI preview' }
        elseif ($isActive) { 'Active asset' }
        elseif ($isArtworkComparison) { 'Artwork comparison' }
        elseif ($relativePath -match '(?i)redesigns.*old') { 'Archived reference' }
        else { 'Concept / reference' }

    if ($name -match '^[0-9a-f]{8}-[0-9a-f-]{27,}$') {
        $title = 'Reference artwork - ' + $name.Substring(0, 8)
    }
    else {
        $title = $name -replace '[_-]+', ' '
        $title = (Get-Culture).TextInfo.ToTitleCase($title.ToLowerInvariant())
    }

    $imageUri = $baseUri.MakeRelativeUri([Uri]$file.FullName).ToString()
    $escapedTitle = [Net.WebUtility]::HtmlEncode($title)
    $escapedPath = [Net.WebUtility]::HtmlEncode($relativePath)
    $escapedStatus = [Net.WebUtility]::HtmlEncode($status)
    $categoryList = $categories -join ' '
    $searchText = [Net.WebUtility]::HtmlEncode(($title + ' ' + $relativePath).ToLowerInvariant())

    @"
<article class="card" data-categories="$categoryList" data-search="$searchText">
  <button class="image-button" type="button" aria-label="Open $escapedTitle">
    <img src="$imageUri" alt="$escapedTitle" loading="lazy">
    <span class="dimensions">Loading dimensions...</span>
  </button>
  <div class="card-info">
    <h2>$escapedTitle</h2>
    <div class="badges"><span class="status">$escapedStatus</span>$($categories | ForEach-Object { "<span class=`"category`">$_</span>" })</div>
    <code>$escapedPath</code>
  </div>
</article>
"@
}

$cardsHtml = $cards -join "`n"
$template = @'
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Taum's Slime Ranching - Review Gallery</title>
  <style>
    :root { color-scheme: dark; --ink: #f1e8ff; --muted: #b7adc9; --panel: #171522; --panel2: #211b31; --edge: #403552; --accent: #d18aff; --mint: #9fe6cc; }
    * { box-sizing: border-box; }
    body { margin: 0; min-height: 100vh; color: var(--ink); background: radial-gradient(ellipse at top, #28203b, #111019 58%); font: 15px/1.45 system-ui, Segoe UI, sans-serif; }
    header { max-width: 1500px; margin: 0 auto; padding: 30px 28px 18px; }
    h1 { margin: 0; font-size: clamp(1.7rem, 3vw, 2.5rem); letter-spacing: .02em; }
    header p { max-width: 850px; margin: 8px 0 20px; color: var(--muted); }
    .toolbar { display:flex; gap:12px; flex-wrap:wrap; align-items:center; }
    .filters { display:flex; gap:7px; flex-wrap:wrap; }
    button, input { font: inherit; }
    .filter { border:1px solid var(--edge); border-radius:999px; padding:8px 14px; color:var(--ink); background:var(--panel2); cursor:pointer; }
    .filter.active { color:#20152d; background:var(--accent); border-color:var(--accent); font-weight:700; }
    #search { min-width:220px; flex:1; max-width:440px; border:1px solid var(--edge); border-radius:10px; padding:10px 13px; color:var(--ink); background:#100e18; }
    #count { color:var(--muted); }
    main { max-width:1500px; margin:auto; padding:4px 28px 40px; }
    .grid { display:grid; grid-template-columns:repeat(auto-fill,minmax(min(100%,330px),1fr)); gap:16px; }
    .card { min-width:0; overflow:hidden; border:1px solid var(--edge); border-radius:14px; background:linear-gradient(145deg,var(--panel2),var(--panel)); box-shadow:0 8px 26px #0003; }
    .image-button { display:block; position:relative; width:100%; height:290px; padding:12px; border:0; border-bottom:1px solid var(--edge); background-color:#12111a; background-image:linear-gradient(45deg,#201d29 25%,transparent 25%),linear-gradient(-45deg,#201d29 25%,transparent 25%),linear-gradient(45deg,transparent 75%,#201d29 75%),linear-gradient(-45deg,transparent 75%,#201d29 75%); background-size:24px 24px; background-position:0 0,0 12px,12px -12px,-12px 0; cursor:zoom-in; }
    .image-button img { width:100%; height:100%; object-fit:contain; image-rendering:auto; }
    .dimensions { position:absolute; right:9px; bottom:8px; padding:3px 7px; border-radius:6px; color:#eee; background:#000b; font:11px ui-monospace,Consolas,monospace; }
    .card-info { padding:12px 14px 15px; }
    .card h2 { margin:0 0 8px; font-size:1rem; }
    .badges { display:flex; gap:5px; flex-wrap:wrap; margin-bottom:9px; }
    .badges span { border:1px solid #ffffff20; border-radius:999px; padding:2px 8px; color:var(--muted); font-size:.72rem; }
    .badges .status { color:var(--mint); border-color:#9fe6cc55; }
    code { display:block; overflow-wrap:anywhere; color:#a99ebd; font-size:.72rem; }
    #empty { display:none; padding:34px; border:1px dashed var(--edge); border-radius:12px; color:var(--muted); text-align:center; }
    dialog { width:min(96vw,1500px); max-width:none; max-height:96vh; padding:12px; border:1px solid var(--edge); border-radius:14px; color:var(--ink); background:#111019; }
    dialog::backdrop { background:#05040bdc; }
    dialog img { display:block; max-width:100%; max-height:84vh; margin:auto; object-fit:contain; }
    .modal-head { display:flex; justify-content:space-between; align-items:center; gap:12px; padding:0 4px 10px; color:var(--muted); }
    .modal-head button { border:1px solid var(--edge); border-radius:8px; padding:5px 10px; color:var(--ink); background:var(--panel2); cursor:pointer; }
    @media(max-width:600px){ header,main{padding-left:14px;padding-right:14px} .image-button{height:230px} }
  </style>
</head>
<body>
  <header>
    <h1>Taum's Slime Ranching - Review Gallery</h1>
    <p>Browse current mod assets and concept references without launching Stardew Valley. The UI section includes static previews of the implemented C# menu layouts and text; game texture boxes, fonts, and live slime sprites are represented schematically. Active assets are marked in green; concepts and archived references are labelled separately. Run <code>scripts/Generate-Review-Gallery.ps1</code> to refresh this page after adding or changing artwork.</p>
    <div class="toolbar">
      <nav class="filters" aria-label="Asset sections">
        <button class="filter active" data-filter="All">All</button>
        <button class="filter" data-filter="UI">UI</button>
        <button class="filter" data-filter="Furniture">Furniture</button>
        <button class="filter" data-filter="Machines">Machines</button>
        <button class="filter" data-filter="Buildings">Buildings</button>
        <button class="filter" data-filter="Slimes">Slimes</button>
      </nav>
      <input id="search" type="search" placeholder="Search names or file paths..." aria-label="Search assets">
      <span id="count"></span>
    </div>
  </header>
  <main>
    <section class="grid" id="grid">
__CARDS__
    </section>
    <div id="empty">No matching images. Try a different section or search.</div>
  </main>
  <dialog id="viewer"><div class="modal-head"><span id="modal-title"></span><button id="close" type="button">Close [X]</button></div><img id="modal-image" alt=""></dialog>
  <script>
    const cards = [...document.querySelectorAll('.card')];
    const filters = [...document.querySelectorAll('.filter')];
    const search = document.querySelector('#search');
    const count = document.querySelector('#count');
    const empty = document.querySelector('#empty');
    let activeFilter = 'All';
    function refresh() {
      const query = search.value.trim().toLowerCase();
      let shown = 0;
      for (const card of cards) {
        const categories = card.dataset.categories.split(' ');
        const visible = (activeFilter === 'All' || categories.includes(activeFilter)) && card.dataset.search.includes(query);
        card.hidden = !visible;
        if (visible) shown++;
      }
      count.textContent = `${shown} image${shown === 1 ? '' : 's'}`;
      empty.style.display = shown ? 'none' : 'block';
    }
    filters.forEach(button => button.addEventListener('click', () => {
      activeFilter = button.dataset.filter;
      filters.forEach(item => item.classList.toggle('active', item === button));
      refresh();
    }));
    search.addEventListener('input', refresh);
    document.querySelectorAll('.card img').forEach(img => img.addEventListener('load', () => {
      img.closest('.image-button').querySelector('.dimensions').textContent = `${img.naturalWidth} x ${img.naturalHeight}`;
    }));
    const viewer = document.querySelector('#viewer');
    document.querySelectorAll('.image-button').forEach(button => button.addEventListener('click', () => {
      const image = button.querySelector('img');
      document.querySelector('#modal-image').src = image.src;
      document.querySelector('#modal-image').alt = image.alt;
      document.querySelector('#modal-title').textContent = image.alt;
      viewer.showModal();
    }));
    document.querySelector('#close').addEventListener('click', () => viewer.close());
    viewer.addEventListener('click', event => { if (event.target === viewer) viewer.close(); });
    refresh();
  </script>
</body>
</html>
'@

$html = $template.Replace('__CARDS__', $cardsHtml)
Set-Content -LiteralPath $outputPath -Value $html -Encoding UTF8
Write-Output "Generated review gallery with $(@($cards).Count) image(s): $outputPath"
if ($skippedLongPaths.Count -gt 0) {
    Write-Output "Skipped $($skippedLongPaths.Count) file(s) with paths longer than Windows can reliably open."
}
