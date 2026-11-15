"""Round 11 item 1: Buck "Big Finish" Barlow, the tour's organiser (Docs/STORY.md). TeedOff Barker calls the races; Buck talks
before and after them (briefings, results remarks) and in The Tour's scenes. They never talk over each other.

His ElevenLabs voice is "ShiftStorm Buck" (Dan made it, 2026-11-15; voice design prompt in Docs/STORY.md); model eleven_v3 with
[tags], like Barker's lines (Tools/Audio/caller_lines.py, whose api_key() and to_ogg() are reused: trimmed, -16 LUFS, mono Vorbis).

Dry run by default (lists the lines and the estimated credits). --go records the missing ones into
Assets/Resources/Audio/Voice/buck/<id>.ogg and writes Assets/Resources/Audio/Voice/buck/lines.json (id -> words: the subtitles and
the pools; Race/Buck.cs). The key comes from ELEVENLABS_API_KEY; it is never printed or written anywhere.

Pools (by prefix, Race/Buck.cs):
  brief_pp_*, brief_cc_*         pre-race briefing per course (10-20 s)
  short_pp_*, short_cc_*         the "Short" briefing setting (one sentence)
  res_<pp|cc>_<first|podium|back>_*   results remark by the player's place
  yarn_*                         anecdotes / safety-briefing jokes, now and then after a briefing (not every race)
  tour_*                         The Tour's scenes (Race/Tour.cs): each scene is a few lines in order
"""
import argparse
import json
import os
import sys
import urllib.error
import urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import caller_lines  # noqa: E402  (api_key, to_ogg)

REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(REPO, "Assets", "Resources", "Audio", "Voice", "buck")
VOICE_NAME = "ShiftStorm Buck"
VOICE_ID = "aFRmECDej7JOyV0rztX7"   # "ShiftStorm Buck" (looked up by name on 2026-11-15)
MODEL = "eleven_v3"

LINES = {
    # ---- Paradise Plunge: the private trial ("a gentle introductory outing") ----
    "brief_pp_1": "[warmly] Welcome, welcome, one and all! Now, this is just a gentle introductory outing. A little drive, a little float, "
                  "a little flight, a little dip. I checked every ramp myself. [chuckles] Twice. Mostly.",
    "brief_pp_2": "[cheerfully] Folks, a word about the wildlife. The big fellow's a plant eater. Lovely disposition. Now, the other fellow... "
                  "[pause] let's keep moving. Engines on!",
    "brief_pp_3": "[proudly] Car, boat, plane and submarine. One machine, four ways to have a wonderful afternoon. "
                  "If it changes shape on you, that means it's working. If it doesn't... [chuckles] keep your elbows in.",
    "brief_pp_4": "[warmly] This is a private trial, just us and a few curious dinosaurs. Nobody's watching. [pause] "
                  "Which is a shame, because I've got a feeling this is going to be spectacular.",
    "short_pp_1": "[cheerfully] Gentle introductory outing, folks. Mind the dinosaurs.",
    "short_pp_2": "[warmly] Four shapes, one lap. Off you go!",
    # ---- Coaster Coast: the tour's public opening night (Buck's comeback; he did stunt shows here) ----
    "brief_cc_1": "[excited] Ladies and gentlemen, opening night! Oh, no. We're not jumping over the Ferris wheel. That'd be irresponsible. "
                  "[pause] We're going through it.",
    "brief_cc_2": "[nostalgic] I used to jump six buses right over there. Seven, according to the posters. We counted the snack wagon. "
                  "[warmly] Tonight it's your turn to give these folks a show.",
    "brief_cc_3": "[cheerfully] The roller coaster is perfectly safe. It's been tested by professionals. [pause] Well, by me. "
                  "I used to be a professional. Hold on, lean in, and enjoy the fireworks!",
    "brief_cc_4": "[proudly] A real crowd tonight, folks. Fireworks, string lights, the whole pier lit up. "
                  "[softer] I haven't seen this park this full since my last show. Let's give them something to remember.",
    "short_cc_1": "[excited] Opening night, folks! Through the wheel, not over it!",
    "short_cc_2": "[cheerfully] Coaster, flume, sea and pier. Have fun out there!",
    # ---- results remarks by the player's place ----
    "res_pp_first_1": "[delighted] First place! I told everyone this was a gentle outing. [chuckles] You made it look gentle.",
    "res_pp_first_2": "[proudly] A win on the very first trial! Somebody fetch me a bigger poster.",
    "res_pp_first_3": "[warmly] Well, look at that. Not a scratch on the machine, and only a little dinosaur drool. Wonderful!",
    "res_pp_podium_1": "[cheerfully] A podium finish! And all four shapes came back. That's a successful trial in my book.",
    "res_pp_podium_2": "[warmly] Very nice driving. The plant eater was very impressed. The other fellow, I didn't ask.",
    "res_pp_podium_3": "[chuckles] On the podium! See? Gentle introductory outing. Nothing to worry about.",
    "res_pp_back_1": "[kindly] Back of the pack, but you finished, and you finished in one piece. That counts for a lot around here.",
    "res_pp_back_2": "[cheerfully] Don't you worry. My first stunt, I jumped one bus. [pause] Well, into one bus. Try again!",
    "res_pp_back_3": "[warmly] Everybody starts somewhere. I started in a cannon. Let's have another go.",
    "res_cc_first_1": "[excited] First place on opening night! Listen to that crowd! [chuckles] They haven't cheered like that since 1987.",
    "res_cc_first_2": "[proudly] A winner! Through the wheel, over the gap and under the pier. The posters will need a bigger font.",
    "res_cc_first_3": "[delighted] Now that's a big finish! I may have to give you my nickname. [pause] Just borrow it. Just for tonight.",
    "res_cc_podium_1": "[cheerfully] On the podium on opening night! The crowd loved it. So did the octopus, I think.",
    "res_cc_podium_2": "[warmly] Lovely racing. You rode that coaster like you were born on it. Were you? No? Well, close enough.",
    "res_cc_podium_3": "[proudly] A podium! The fireworks were for you, by the way. [pause] And partly for me.",
    "res_cc_back_1": "[kindly] Not this time, but the crowd's still clapping. Opening nights are like that. Shall we go again?",
    "res_cc_back_2": "[chuckles] The coaster takes a little getting used to. My first ride, I came off at the top. On purpose. Mostly.",
    "res_cc_back_3": "[warmly] Chin up! Every great show has a rehearsal. That was the rehearsal.",
    # ---- anecdotes and safety-briefing jokes, now and then ----
    "yarn_1": "[cheerfully] A quick safety note: the signs say 'no flying over the crowd'. [pause] Somebody fixed my signs again. They used to say 'please'.",
    "yarn_2": "[nostalgic] When I was your age I jumped a canyon on a lawnmower. [chuckles] Small canyon. Very big lawnmower.",
    "yarn_3": "[warmly] Safety first, folks. That's why I always do the dangerous bits second.",
    "yarn_4": "[proudly] They called me Big Finish because I never once stopped halfway. [pause] Started halfway, once or twice.",
    "yarn_5": "[cheerfully] If anything goes wrong, remember: it's not a crash, it's an unscheduled transformation.",
    "yarn_6": "[nostalgic] I once raced a hot air balloon across a lake. I won. [pause] The balloon was going the other way, but I won.",
    # ---- The Tour: stop 1, The Trial (Paradise Plunge) ----
    "tour_s1_intro_1": "[warmly] Well, hello there! You must be one of my racers. Buck Barlow. They call me Big Finish. [chuckles] Long story.",
    "tour_s1_intro_2": "[excited] I've had a dream for years, a racing tour where one machine drives, sails, flies and dives, all in one lap.",
    "tour_s1_intro_3": "[cheerfully] Before we go public, a private trial. A gentle introductory outing at a lovely little jungle course.",
    "tour_s1_intro_4": "[proudly] Finish in the top three and the tour's on. Don't mind the dinosaurs. [pause] Most of them are vegetarian.",
    "tour_s1_pass_1": "[delighted] That's it! That's the tour! I knew it! [chuckles] I'm calling the park tonight.",
    "tour_s1_pass_2": "[warmly] You've earned your spot, partner. Next stop: Coaster Coast, and this time there'll be a crowd.",
    "tour_s1_retry_1": "[kindly] Not quite the top three. That's all right. The trial's for learning. The dinosaurs will wait.",
    "tour_s1_retry_2": "[cheerfully] Take another run at it, and keep an eye on that route strip at the top. It never lies. Unlike my posters.",
    # ---- The Tour: stop 2, Opening Night (Coaster Coast) ----
    "tour_s2_intro_1": "[nostalgic] Coaster Coast. I did my stunt shows on this very pier. The owner still owes me a hot dog.",
    "tour_s2_intro_2": "[excited] Tonight it's the tour's public opening. Grandstands, fireworks, the whole town watching.",
    "tour_s2_intro_3": "[cheerfully] The route goes up the roller coaster, through the Ferris wheel, down the log flume and under the pier. Perfectly safe.",
    "tour_s2_intro_4": "[warmly] Top three keeps the tour rolling. [pause] No pressure. Well, a little pressure. It's a show!",
    "tour_s2_pass_1": "[delighted] Listen to them! That's the sound of a tour that's here to stay!",
    "tour_s2_pass_2": "[proudly] Folks are already asking where we go next. [chuckles] I've got a few ideas. Most of them are probably fine.",
    "tour_s2_retry_1": "[kindly] Opening nights are tricky. The crowd's still here, the coaster's still running. Let's go again.",
    "tour_s2_retry_2": "[cheerfully] Ride the coaster all the way and take the wheel's middle if you dare. You've got this.",
    "tour_done_1": "[warmly] Two stops, two shows, and a tour that's really happening. Thank you, partner. [pause] Now, about that volcano...",
}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--go", action="store_true")
    ap.add_argument("--cap", type=int, default=12000)
    ap.add_argument("--redo", nargs="*", help="re-record these ids (or all with no ids)")
    args = ap.parse_args()
    os.makedirs(OUT, exist_ok=True)
    redo = set(LINES) if args.redo == [] else set(args.redo or [])
    todo = [(k, t) for k, t in LINES.items() if k in redo or not os.path.exists(os.path.join(OUT, k + ".ogg"))]
    print(f"{len(LINES)} Buck lines, {len(todo)} to record (about {sum(len(t) for _, t in todo)} credits)")
    if not args.go:
        for k, t in todo:
            print(f"  {k}: {t}")
        print("dry run: add --go to record")
        return
    key = caller_lines.api_key()
    if not key:
        print("ELEVENLABS_API_KEY is not set. Nothing recorded."); sys.exit(2)
    spent = 0
    for k, text in todo:
        if spent + len(text) > args.cap:
            print(f"STOP at {k}: cap {args.cap}"); break
        body = {"text": text, "model_id": MODEL}
        req = urllib.request.Request(f"https://api.elevenlabs.io/v1/text-to-speech/{VOICE_ID}?output_format=mp3_44100_128", data=json.dumps(body).encode("utf-8"), method="POST")
        req.add_header("xi-api-key", key); req.add_header("Content-Type", "application/json")
        try:
            with urllib.request.urlopen(req, timeout=180) as r:
                audio = r.read(); cost = r.headers.get("character-cost")
        except urllib.error.HTTPError as e:
            print(f"  {k}: HTTP {e.code} {e.read().decode('utf-8', 'replace')[:200]}")
            if e.code in (401, 403): break
            continue
        caller_lines.to_ogg(audio, os.path.join(OUT, k + ".ogg"))
        used = int(cost) if cost and cost.isdigit() else len(text); spent += used
        print(f"  {k}: {used} credits (total {spent})", flush=True)
    import re
    words = {k: re.sub(r"\s*\[[^\]]*\]\s*", " ", t).strip().replace("  ", " ") for k, t in LINES.items()}
    with open(os.path.join(OUT, "lines.json"), "w", encoding="utf-8") as f:
        json.dump({"speaker": "Buck Barlow", "lines": [{"id": k, "text": v} for k, v in sorted(words.items())]}, f, indent=1)
    print(f"done: {spent} credits; lines.json written")


if __name__ == "__main__":
    main()
