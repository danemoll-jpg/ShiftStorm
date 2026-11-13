"""ShiftStorm: stage one round's release locally (no upload). Puts the launcher beside the game (Launcher/ShiftStormLauncher.exe +
launcher.json, so installed launchers update themselves), runs Prepare-LauncherRelease.py (signed game manifest, soundtrack manifest,
update catalog, game.zip), then Package-LauncherStarter.py (the complete starter ZIP Dan installs from), and merges the starter ZIP into the
same draft so one GitHub release carries both. Publishing is a separate, deliberate step:
  python Tools/Publish-LauncherRelease.py Builds/Release/build-<N> --publish --confirm-repository danemoll-jpg/ShiftStorm-Releases
Usage: python Tools/Release-Round.py --version 0.1.0 --build 1 --notes "Round 1: ..." """
import argparse, importlib.util, json, pathlib, shutil, subprocess, sys
ROOT = pathlib.Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('release', ROOT / 'Tools/Prepare-LauncherRelease.py'); r = importlib.util.module_from_spec(spec); spec.loader.exec_module(r)
sys.path.insert(0, str(ROOT / 'Launcher'))

def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--version', required=True); p.add_argument('--build', type=int, required=True); p.add_argument('--notes', required=True)
    p.add_argument('--music-build', type=int, default=1)
    a = p.parse_args()
    game = ROOT / 'Builds/ShiftStorm'; out = ROOT / f'Builds/Release/build-{a.build}'
    if out.exists(): raise SystemExit(f'{out} exists: releases are immutable; use the next build number')
    launcher = ROOT / 'Builds/Launcher/ShiftStormLauncher.exe'
    core = (ROOT / 'Launcher/core.hpp').read_text(encoding='utf-8'); lb = int(core.split('LauncherBuild=')[1].split(';')[0])
    (game / 'Launcher').mkdir(exist_ok=True); shutil.copy2(launcher, game / 'Launcher/ShiftStormLauncher.exe')
    (game / 'Launcher/launcher.json').write_text(json.dumps({'build': lb}), encoding='utf-8')
    music = ROOT / 'BundleMusic'
    stage = out / 'prepare'
    subprocess.run([sys.executable, str(ROOT / 'Tools/Prepare-LauncherRelease.py'), '--game', str(game), '--bundle-music', str(music), '--out', str(stage),
                    '--version', a.version, '--build', str(a.build), '--music-build', str(a.music_build), '--notes', a.notes], check=True)
    starter = out / 'starter-draft'
    title = f'ShiftStorm {a.version}'
    subprocess.run([sys.executable, str(ROOT / 'Tools/Package-LauncherStarter.py'), '--game', str(stage / f'starter/versions/{a.build}'),
                    '--game-manifest', str(stage / 'assets/game-manifest.json'), '--music', str(stage / f'starter/shared/soundtracks/{a.music_build}'),
                    '--music-manifest', str(stage / 'assets/soundtrack-manifest.json'), '--out', str(starter), '--zip-name', f'ShiftStorm-{a.version}-Full.zip',
                    '--tag', f'build-{a.build}', '--title', title, '--notes', a.notes,
                    '--cover', str(ROOT / 'SourceArt/Title/ShiftStormCover.png'), '--icon', str(ROOT / 'SourceArt/Title/GameIcon.png'),
                    '--poster', str(ROOT / 'SourceArt/Title/ShiftStormPoster.png')], check=True)
    # one draft: the update assets plus the starter ZIP
    assets = stage / 'assets'
    for z in (starter / 'assets').glob('*.zip'): shutil.copy2(z, assets / z.name)
    inv = json.loads((stage / 'INVENTORY.json').read_text(encoding='utf-8'))
    final = out / 'draft'; shutil.copytree(assets, final / 'assets')
    inventory = dict(tag=f'build-{a.build}', title=title, latest=True, game=dict(version=a.version, build=a.build, notes=a.notes),
                     assets=r.inventory(final / 'assets'), publication='LOCAL DRAFT ONLY')
    r.write(final / 'INVENTORY.json', inventory)
    shutil.rmtree(stage / 'starter', ignore_errors=True); shutil.rmtree(starter / 'work', ignore_errors=True)
    print(json.dumps(dict(draft=str(final), assets=[(x['path'], x['bytes']) for x in inventory['assets']])))

if __name__ == '__main__':
    main()
