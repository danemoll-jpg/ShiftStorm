"""Round 7 item 5b: Barnaby (Barnaby, Tee'd Off's carnival barker) calls the race.

His new racing lines, made with ElevenLabs in his own Tee'd Off voice ("TeedOff Barker", the voice Tee'd Off's
Tools/Audio/voices.json uses for "barker" and "barnaby"; model eleven_v3 with [tags], as Tee'd Off's elevenlabs_voice.py).
His existing Tee'd Off clips that fit a race are copied by copy_existing() (Tee-dOff is read-only: copied, never changed).

Dry run by default (lists the lines and the estimated credits). --go records the missing ones into
Assets/Resources/Audio/Voice/barker/<id>.ogg and writes Assets/Resources/Audio/Voice/barker/lines.json (id -> words, for the
subtitles and the Caller's pools). The key comes from ELEVENLABS_API_KEY (environment or HKCU\\Environment); it is never
printed or written anywhere.
"""
import argparse
import json
import os
import shutil
import subprocess
import sys
import urllib.error
import urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(REPO, "Assets", "Resources", "Audio", "Voice", "barker")
TEEDOFF = r"C:\Users\danmo\Tee-dOff\Assets\_Project\Resources\Audio\Voice\barker"
VOICE_ID = "l7vIj65RwKyM6W9qAV3G"   # "TeedOff Barker" (Tee'd Off Tools/Audio/voices.json)
MODEL = "eleven_v3"
FFMPEG = shutil.which("ffmpeg") or "ffmpeg"

# pool_n: the Caller picks from each pool by its prefix (Assets/Scripts/Race/Caller.cs)
LINES = {
    "start_1": "[showman] Ladies and gentlemen, start your engines! And your wings! And your propellers!",
    "start_2": "[showman] Step right up, folks! Four racers, one lap, and every shape a machine can take!",
    "start_3": "[showman] Welcome to Paradise Plunge! Road, sky, lagoon and the deep blue below!",
    "go_1": "[shouting] And they're off!",
    "go_2": "[showman] Go, go, go! Off they go, folks!",
    "lead_1": "[showman] A new leader, folks! Hold on to your hats!",
    "lead_2": "[excited] Out in front! Did you see that pass?",
    "lead_3": "[showman] And the lead changes hands! What a race!",
    "loop_1": "[delighted] Round and round and right side up again! Spectacular!",
    "loop_2": "[showman] The loop-the-loop! Don't look down... or up!",
    "loop_3": "[gasps] Upside down at full throttle! Marvelous!",
    "loopfail_1": "[sympathetic] Ohhh, not enough speed for the loop! Back you go!",
    "jump_1": "[showman] Up, up and over! What airtime, folks!",
    "jump_2": "[excited] Look at it fly! And that's with wheels!",
    "jump_3": "[delighted] Sticks the landing! Give that driver a hand!",
    "wings_1": "[showman] Wings out! The scenic route over the lake!",
    "wings_2": "[excited] It's a car! It's a plane! It's both, folks!",
    "stay_1": "[showman] No wings for this one! Keeping it on four wheels!",
    "stay_2": "[chuckles] Who needs wings? Not this daredevil!",
    "shortcut_1": "[sly] Ooh, a sneaky shortcut! I didn't see a thing, folks.",
    "shortcut_2": "[showman] Through the secret passage! Clever, clever!",
    "miss_1": "[showman] Close, but no cigar! That's a three second penalty!",
    "miss_2": "[sympathetic] Ohhh, just wide of the gate! Penalty time, folks!",
    "wall_1": "[wincing] Ooh! That's gonna leave a mark!",
    "wall_2": "[chuckles] Bonk! The wall wins that round, folks!",
    "wall_3": "[showman] Mind the scenery, it was here first!",
    "splash_1": "[chuckles] Into the creek! A little swim never hurt anyone!",
    "final_1": "[excited] Here comes the final stretch! Who wants it most?",
    "final_2": "[showman] Back on the road and heading for home, folks!",
    "photo_1": "[shouting] It's a photo finish! Somebody get me a camera!",
    "photo_2": "[excited] Neck and neck to the line! Too close to call!",
    "win_abi": "[delighted] Abi takes it! The crowd goes wild!",
    "win_rae": "[showman] Rocket Rae takes the chequered flag!",
    "win_chip": "[showman] Chip Showtime Sterling wins it! Showtime indeed!",
    "win_vivi": "[showman] Vivi wins! What a performance, folks!",
    "win_p2": "[showman] Player two takes the crown! Winner, winner!",
    "results_1": "[showman] Thank you, thank you! Come back for the next race, folks!",
    "results_2": "[showman] Step right up and see the results!",
    "trial_1": "[showman] One racer, one clock, no excuses! Let's see that time, folks!",
    # ---- Round 8 item 2: he talks a lot more (a line every 6-10 s of racing): every form change, overtakes by rival name, places,
    # near misses and penalties, walls, the loop, each jump, landmarks and dinosaurs, close racing, the last stretch, the finish by place
    "trial_2": "[showman] The clock is ticking, folks! Every second counts on this island!",
    "start_4": "[showman] Hold on to your helmets! Paradise Plunge is about to go wild!",
    "go_3": "[shouting] Green light! Put the pedal down!",
    "tocar_1": "[showman] Wheels down! Back on the blacktop, folks!",
    "tocar_2": "[excited] And it's a car again! Rubber meets the road!",
    "tocar_3": "[delighted] Out of the water and onto the road! What a machine!",
    "tocar_4": "[showman] Four wheels and a dream, ladies and gentlemen!",
    "toboat_1": "[showman] Splashdown! Now it's a boat!",
    "toboat_2": "[excited] Hull down, engines roaring, it's boat time, folks!",
    "toboat_3": "[delighted] Into the lagoon with a splash! Who brought towels?",
    "toboat_4": "[showman] Ahoy there! Full speed ahead through the water!",
    "toplane_1": "[shouting] Wings out! Up, up and away!",
    "toplane_2": "[excited] It's a plane now, folks! Mind the pterodactyls!",
    "toplane_3": "[showman] Off the ramp and into the sky! Spectacular!",
    "toplane_4": "[delighted] Look at it soar! The clouds are jealous, folks!",
    "tosub_1": "[showman] Down periscope! It's a submarine!",
    "tosub_2": "[excited] Diving deep into the flooded caves, folks!",
    "tosub_3": "[mysterious] Into the deep blue, where the fishies don't pay rent!",
    "tosub_4": "[showman] Hatches sealed! Glub glub, ladies and gentlemen!",
    "passrae_1": "[excited] Right past Rocket Rae! Rae won't like that one bit!",
    "passrae_2": "[showman] And Rocket Rae gets left in the dust!",
    "passchip_1": "[excited] Past Chip Sterling! Sorry Chip, the show goes on!",
    "passchip_2": "[chuckles] Chip gets passed! That's not in his script, folks!",
    "passvivi_1": "[excited] Past Vivi! Elegant, but not fast enough!",
    "passvivi_2": "[showman] Vivi gets overtaken! She'll be furious, darling!",
    "byrae_1": "[excited] Rocket Rae muscles her way past! Look at those biceps go!",
    "byrae_2": "[showman] Here comes Rocket Rae! Full power, no brakes!",
    "bychip_1": "[excited] Chip Showtime Sterling slides on by! And he waves to the crowd!",
    "bychip_2": "[showman] Chip takes the place! Somebody cue the spotlight!",
    "byvivi_1": "[excited] Vivi glides past! Graceful as a swan, fast as a rocket!",
    "byvivi_2": "[showman] And Vivi takes the place! Without a hair out of place!",
    "up_1": "[excited] Up a place! Keep it coming!",
    "up_2": "[showman] Climbing the order, folks! Who can stop them?",
    "up_3": "[delighted] Another place gained! The crowd loves a comeback!",
    "down_1": "[sympathetic] Ohhh, dropped a place! Plenty of island left, folks!",
    "down_2": "[showman] Lost a spot there! Time to dig deep!",
    "down_3": "[chuckles] Slipping back! Don't let them get away!",
    "close_1": "[excited] Side by side, folks! Nobody blink!",
    "close_2": "[showman] Wheel to wheel, wing to wing! This is racing!",
    "close_3": "[excited] Inches apart! I can't watch, but I can't look away!",
    "behind_1": "[showman] The pack's getting away! Time to find some speed!",
    "behind_2": "[sympathetic] Bringing up the rear, folks! But the race isn't over!",
    "behind_3": "[showman] A long way back! Hit that boost, my friend!",
    "catch_1": "[excited] Closing the gap! They're reeling them in, folks!",
    "catch_2": "[showman] Catching up fast! Somebody's been eating their vegetables!",
    "catch_3": "[excited] The gap is shrinking! Here they come!",
    "creek_1": "[showman] Over the creek! Don't look down, the fish are watching!",
    "creek_2": "[excited] Creek Jump! Splash-free, folks!",
    "cliff_1": "[shouting] The Cliff Gap! Fly, little car, fly!",
    "cliff_2": "[excited] Over the ravine! My heart's in my mouth, folks!",
    "boost_1": "[excited] Boost! Hold on to something!",
    "boost_2": "[showman] Kaboom! That's the boost, folks!",
    "trex_1": "[shouting] Look out for the T. rex! Don't feed it, folks!",
    "trex_2": "[nervous] Is that T. rex smiling? I think it's smiling!",
    "trex_3": "[showman] The king of the cliffs is watching! Wave, but not too much!",
    "bronto_1": "[delighted] The brontosaurus herd is out for a stroll! Aren't they lovely?",
    "bronto_2": "[chuckles] Mind the long necks, folks! They have the right of way!",
    "raptor_1": "[excited] Raptors running alongside! Are they racing too?",
    "raptor_2": "[nervous] Those raptors are keeping up! Drive faster, please!",
    "ptero_1": "[showman] Pterodactyls overhead! Watch your windscreen, folks!",
    "ptero_2": "[excited] Flying with the pterodactyls! They want a race!",
    "plesio_1": "[delighted] A long-neck in the lagoon! Say hello, folks!",
    "plesio_2": "[mysterious] Something big is swimming down there! Don't splash it!",
    "trike_1": "[chuckles] Triceratops in the mangroves! Three horns and zero patience!",
    "trike_2": "[showman] Steer clear of the triceratops, folks! They don't do selfies!",
    "nest_1": "[delighted] Look at the nests! Dino eggs as big as beach balls!",
    "skeleton_1": "[mysterious] Fossils in the walls, folks! Don't end up as one!",
    "arch_1": "[showman] Through the Sky Arch! Thread the needle, folks!",
    "arch_2": "[excited] The Sky Arch! Biggest rock in Paradise!",
    "lagoon_1": "[showman] Lagoon Landing! Hope you packed your swimsuit!",
    "mangrove_1": "[showman] Into the Mangrove Run! Mind the roots, folks!",
    "mangrove_2": "[excited] Twisting through the swamp! It's a jungle in there!",
    "gateway_1": "[mysterious] The Sunken Gateway! Ancient stones, ancient secrets!",
    "vault_1": "[mysterious] The Coral Vault! Glowing crystals and things that go bump!",
    "vault_2": "[showman] Down in the deep! Watch those arches, folks!",
    "waterfall_1": "[shouting] Through the waterfall! Free car wash, folks!",
    "waterfall_2": "[excited] Bursting through the falls! Spectacular!",
    "canyon_1": "[showman] The car line through the jungle canyon! Past the long necks!",
    "canyon_2": "[excited] Down the canyon road! Rocks on the left, dinos on the right!",
    "wall_4": "[wincing] Ooh! The scenery fights back, folks!",
    "wall_5": "[chuckles] Boing! Somebody check the paintwork!",
    "wall_6": "[sympathetic] That's gonna cost a few seconds!",
    "miss_4": "[showman] Missed the gate! Three seconds in the penalty box, folks!",
    "miss_5": "[sympathetic] So close to the gate! The judges say penalty!",
    "loop_4": "[shouting] Loop-de-loop! Gravity takes a holiday, folks!",
    "loopfail_2": "[sympathetic] Not enough speed! The loop wins this round!",
    "loopfail_3": "[chuckles] Halfway up and back down again! Try it with more boost!",
    "jump_5": "[excited] Big air, folks! Big, big air!",
    "wings_3": "[showman] The scenic route! Over the lake through the golden rings!",
    "stay_3": "[showman] Staying a car! Down the canyon road we go!",
    "shortcut_3": "[sly] The secret way! Don't tell the others, folks!",
    "splash_2": "[chuckles] Swimming isn't racing, folks! Back you go!",
    "final_3": "[excited] The finish line is in sight! Give it everything!",
    "fill_1": "[showman] What a day in Paradise, folks! Sun, sea and supercharged engines!",
    "fill_2": "[delighted] Remember, folks, the dinosaurs were here first! Be polite!",
    "fill_3": "[showman] Car, boat, plane and submarine! One machine, four times the fun!",
    "fill_4": "[chuckles] I'd race too, but my hat doesn't fit in the cockpit!",
    "fill_5": "[showman] Step right up and watch the greatest race on the island!",
    "fill_6": "[delighted] Smell that jungle air, folks! A little bit volcano, a little bit dinosaur!",
    "fill_7": "[showman] Keep your hands inside the vehicle at all times! Unless you're waving!",
    "fill_8": "[excited] Every shape a machine can take, and they're taking them all at full speed!",
    "fill_9": "[chuckles] Don't worry about the volcano, folks! It only rumbles on Tuesdays!",
    "fill_10": "[showman] Is it a car? Is it a boat? Folks, it's both, and more!",
    "fill_11": "[delighted] Look at them go! This is why I love my job!",
    "fill_12": "[showman] Ladies and gentlemen, the race is on and the island is watching!",
    "podium_1": "[showman] A podium finish! Step right up and take a bow!",
    "podium_2": "[delighted] Not first, but on the podium! The crowd still loves you!",
    "podium_3": "[showman] A fine finish, folks! A prize from the middle shelf!",
    "last_1": "[sympathetic] Last place, but you finished! Next time, more boost!",
    "last_2": "[chuckles] Somebody has to bring up the rear! Better luck next race, folks!",
    "last_3": "[sympathetic] Ohhh, last across the line! Step right up and try again!",
    "results_3": "[showman] And there we have it! What a race, ladies and gentlemen!",
}
# Round 10: Coaster Coast (the seaside pier park) and a results line naming each course. The Caller uses the "cc" pools on Coaster
# Coast in place of the Paradise Plunge ones that name the jungle, the lake or the dinosaurs (start, fill, wings, splash).
ROUND10 = {
    "ccstart_1": "[showman] Welcome to Coaster Coast! The pier, the park and the deep blue sea, all in one lap!",
    "ccstart_2": "[showman] Step right up, folks! Tonight we race the rides! Coaster, flume, and the sea below!",
    "ccstart_3": "[delighted] The sun is setting on Coaster Coast, and the lights are coming on! Racers, are you ready?",
    "ccfill_1": "[showman] Popcorn, candy floss, and four machines at full throttle! What a night at the pier!",
    "ccfill_2": "[chuckles] No height restriction on this ride, folks! Not even for me!",
    "ccfill_3": "[showman] Keep your arms and legs inside the ride! Unless you're a plane, then use your wings!",
    "ccfill_4": "[delighted] Smell that sea air, folks! A little bit salt, a little bit candy floss!",
    "ccfill_5": "[showman] Every ride in the park, and they're riding them all at once!",
    "ccfill_6": "[excited] The whole pier is shaking! Is that the coaster or the crowd?",
    "ccfill_7": "[chuckles] I asked for one ticket to every ride. They gave me a race car!",
    "ccfill_8": "[showman] Ladies and gentlemen, the lights are on and the park is watching!",
    "ccfill_9": "[delighted] Look at those lights! The whole coast is glowing tonight!",
    "ccfill_10": "[showman] Roll up, roll up! The greatest race on the boardwalk!",
    "ccwheel_1": "[gasps] Right under the big wheel! Wave to the people at the top, folks!",
    "ccwheel_2": "[showman] There goes the Ferris wheel, round and round, and there go our racers, faster still!",
    "ccwings_1": "[showman] Wings out! Straight through the middle of the Ferris wheel!",
    "ccwings_2": "[excited] Through the big wheel! I've never seen anyone ride it like that!",
    "cccoaster_1": "[showman] Onto the roller coaster! Hold on to your hats, this ride has no brakes!",
    "cccoaster_2": "[excited] Up the hill and down the drop! Scream if you want to go faster!",
    "cccoaster_3": "[delighted] Riding the rails like a real coaster car! Wheee!",
    "ccgap_1": "[gasps] Off the end of the track and across the gap! Will it land?",
    "ccgap_2": "[showman] The Coaster Gap! No track, no problem!",
    "cccork_1": "[excited] Into the corkscrew! Upside down and round again!",
    "cccork_2": "[delighted] A barrel roll on the coaster! My popcorn just went everywhere!",
    "cctophat_1": "[showman] All the way to the top of the Top Hat! And now, wings out!",
    "cctophat_2": "[excited] Off the top of the coaster and into the sky! Magnificent!",
    "cchelter_1": "[showman] Round the helter-skelter! Mind the slide, folks!",
    "ccfireworks_1": "[delighted] Through the fireworks! Oooh! Aaah!",
    "ccfireworks_2": "[excited] Fireworks all around them! What a show, folks!",
    "ccballoon_1": "[chuckles] Wave to the big balloon, folks! It's waving back!",
    "ccballoon_2": "[showman] Look at the size of that balloon! Don't pop it!",
    "ccflume_1": "[showman] Splashdown in the log flume! You might get a little wet, folks!",
    "ccflume_2": "[delighted] Down the flume they go, right under the boardwalk!",
    "cclighthouse_1": "[showman] Round the lighthouse, or through the rocks? Choose wisely!",
    "cclighthouse_2": "[excited] The old lighthouse! Keep left of the light, folks!",
    "ccpier_1": "[showman] Under the pier they go! Mind the legs!",
    "ccpier_2": "[delighted] Down beneath the boardwalk, where the old rides sleep!",
    "cccarousel_1": "[gasps] The sunken carousel! The horses are still going round down there!",
    "ccoctopus_1": "[gasps] Is that an octopus? That is a VERY big octopus, folks!",
    "ccoctopus_2": "[chuckles] Eight arms, and not one of them can drive!",
    "ccwhale_1": "[delighted] A whale! Right beside them! The biggest fan in the park!",
    "ccwhale_2": "[excited] Dolphins racing alongside! Somebody give them a lap time!",
    "ccsplash_1": "[chuckles] Into the sea! The pier has edges, folks!",
    "ccpool_1": "[showman] Up through the splash pool and back onto the boardwalk!",
    "ccres_1": "[showman] And that's the race at Coaster Coast! What a night at the pier!",
    "ccres_2": "[delighted] That was Coaster Coast, folks! Same time tomorrow, same rides!",
    # Docs/STORY.md: Coaster Coast is the tour's public opening night (its first public race; Buck comes later)
    "ccopen_1": "[showman] Ladies and gentlemen, it's opening night! The tour's very first public race, right here on the pier!",
    "ccopen_2": "[excited] A real crowd, real fireworks, and the grand opening of the greatest racing tour on earth!",
    "ccopen_3": "[delighted] They tried it out in private in the jungle. Tonight, folks, they race for you!",
    "ccfill_11": "[showman] Grand opening night, folks! Tell your friends you were here for the first one!",
    "ccres_3": "[showman] And that's the first public race of the tour in the books! What an opening night!",
    "ppres_1": "[showman] And that's the race at Paradise Plunge! The dinosaurs approve!",
    "ppres_2": "[delighted] That was Paradise Plunge, folks! Jungle, lagoon and all!",
}
LINES.update(ROUND10)
# Round 11: choose your racer: Abi can be an AI rival now (passed by / passing her, like Rae / Chip / Vivi)
ROUND11 = {
    "passabi_1": "[showman] Right past Abi! The competitor won't take that lying down!",
    "passabi_2": "[excited] And Abi gets overtaken! She'll want that one back!",
    "byabi_1": "[showman] Here comes Abi! Focused, fearless and flying by!",
    "byabi_2": "[excited] Abi takes the place! What a move!",
}
LINES.update(ROUND11)
# Round 13: the rival challenges in The Tour (one on one)
ROUND13 = {
    "chstart_1": "[showman] A grudge match, folks! One on one, and only one of them gets bragging rights!",
    "chstart_2": "[excited] Head to head, nowhere to hide! Let's see who blinks first!",
    "chwon_1": "[delighted] And the challenge goes to the challenger! What a statement!",
    "chlost_1": "[showman] Not this time! The rival holds on, but there's always a rematch, folks!",
}
LINES.update(ROUND13)
# Round 15: Big Top Breakdown (Race/Caller.cs: bt* pools; the set-piece ones only while that thing is big on the screen)
ROUND15 = {
    "btstart_1": "[showman] Ladies and gentlemen, children of all ages! Welcome to the Big Top Breakdown!",
    "btstart_2": "[excited] Roll up, roll up! Four racers, one circus, and a cannon with their names on it!",
    "btstart_3": "[showman] The Lustre Brothers Circus, folks! Mind the wagons, mind the clowns, and mind the elephants!",
    "btfill_1": "[showman] What a show, folks! And nobody's even juggled yet!",
    "btfill_2": "[chuckles] Somebody tell the popcorn man to duck!",
    "btfill_3": "[excited] The greatest race under the sun! The tent's around here somewhere!",
    "btfill_4": "[showman] Hold on to your candy floss, folks, it's getting breezy!",
    "btwings_1": "[showman] Wings out at the Clown Jump! Right over the Globe of Death!",
    "btwings_2": "[excited] Look, no loop! Flying the long way round, folks!",
    "btsplash_1": "[chuckles] Splashdown! The elephants are jealous!",
    "btres_1": "[showman] And that's the show, folks! Give the racers a big top round of applause!",
    "btres_2": "[delighted] What a performance! Take a bow, every one of you!",
    "btjump_1": "[excited] Over the clown cars! Honk honk, folks!",
    "btglobe_1": "[gasps] Into the Globe of Death! Round and round and out the other side!",
    "btglobe_2": "[showman] The Globe of Death, folks! Don't try this at home. Or anywhere!",
    "btcannon_1": "[shouting] Ready... aim... BOOM! Human cannonball, folks!",
    "btcannon_2": "[excited] Fired out of the cannon! Through the hoops, out the top!",
    "btwagons_1": "[showman] Mind the wagon parade! They've got right of way!",
    "btstrongman_1": "[chuckles] Look at the strongman, folks! He's holding up the whole midway!",
    "bttightrope_1": "[gasps] Up on the tightrope! Don't look down, sweetheart!",
    "btclowns_1": "[laughs] Clowns everywhere! How many fit in that little car?",
    "bttent_1": "[showman] Into the big top, folks! Once round the ring and into the cannon!",
    "bttrapeze_1": "[excited] The flying trapeze! They make it look so easy!",
    "btelephants_1": "[delighted] The elephants are giving everyone a shower!",
    "btsteamer_1": "[showman] There's the showboat, folks! Toot toot!",
    "btseals_1": "[chuckles] Sea lions balancing balls! Now that's talent!",
    "btrapids_1": "[sly] Ooh, through the rapids! Hold on to your hat!",
    "bttank_1": "[gasps] The escape artist's tank! Is he still in there?",
    "btsunken_1": "[showman] Through the sunken circus wagon! Mind the clowns' old shoes!",
    "btpool_1": "[excited] Up out of the water and back to the midway! Here comes the finish!",
}
LINES.update(ROUND15)
# Round 16: Big Top's Clown flight called by its hoops; Dam Shame (Race/Caller.cs: dm* pools; the set-piece ones only while that thing is
# big on the screen; dmrise1 / dmrise2 when the water rises, after Buck's tannoy line; dmlap2 / dmlap3 at the lap lines)
ROUND16 = {
    "btwings_3": "[showman] Wings out! Through the hoops and on to the big top!",
    "bthoops_1": "[excited] Through the hoops, folks! One, two, three, four!",
    "dmstart_1": "[excited] Welcome to Dam Shame, folks! Three laps, one dam, and a whole lot of water!",
    "dmstart_2": "[showman] Good morning from the mountains! Buck promises a controlled water demonstration. I promise nothing!",
    "dmstart_3": "[chuckles] Three laps today, folks, and the course changes every one of them. Hold on to your snorkels!",
    "dmfill_1": "[showman] What a morning for it, folks! Not a cloud in the sky. Plenty of water, though.",
    "dmfill_2": "[chuckles] Somebody tell the fish we're coming through!",
    "dmfill_3": "[excited] Concrete, pine trees, and four racers who can't stay in one shape!",
    "dmfill_4": "[sly] I've seen the dam keeper's face, folks. She is not enjoying this.",
    "dmwings_1": "[excited] Wings out off the flip bucket! Over the plunge pool!",
    "dmwings_2": "[showman] Off the lip and into the canyon air, folks!",
    "dmsplash_1": "[chuckles] Splashdown in the valley! That's new!",
    "dmres_1": "[showman] And that's the demonstration, folks! The dam is still standing! Mostly!",
    "dmres_2": "[delighted] Three laps, three courses, one very wet valley! What a race!",
    "dmjump_1": "[excited] Off the flip bucket and over the plunge pool!",
    "dmrise1_1": "[gasps] The spillway's running! Boats down the chute, folks!",
    "dmrise1_2": "[excited] Here comes the water! The valley's going under, just a little!",
    "dmrise2_1": "[shouting] That is NOT a smidge! The whole valley's flooding, folks!",
    "dmrise2_2": "[gasps] It's a lake! The valley is a lake! Subs at the ready!",
    "dmlap2_1": "[excited] Lap two, folks, and the water's up!",
    "dmlap2_2": "[showman] Into lap two! Same dam, different river!",
    "dmlap3_1": "[shouting] Final lap! Under the water we go!",
    "dmlap3_2": "[excited] Last lap, folks! Hope everyone packed a periscope!",
    "dmspill_1": "[excited] Look at that spillway roar! Down the torrent they go!",
    "dmspill_2": "[showman] White water all the way down the chute, folks!",
    "dmgates_1": "[gasps] The spillway gates are open! Buck, what did you do?",
    "dmturbine_1": "[excited] Through the turbine hall! Mind the giant spinning things!",
    "dmturbine_2": "[chuckles] Those turbines are making power, folks. Mostly from the racers.",
    "dmbuck_1": "[chuckles] There's Buck up in the control room, waving! Hands off the buttons, Buck!",
    "dmbeaver_1": "[delighted] Look, folks, the beavers have built their own dam! And theirs isn't leaking!",
    "dmbeaver_2": "[laughs] Beavers! The real engineers on this course!",
    "dmheron_1": "[whispers] Shh, a heron! Fishing, of course. Aren't we all?",
    "dmtrout_1": "[excited] Trout jumping out of the water! They want a look at the race too!",
    "dmlogs_1": "[gasps] Logs in the flood! And a picnic table! And a duck!",
    "dmdam_1": "[showman] Look at the size of that dam, folks! Seven hundred metres of concrete!",
    "dmhall_1": "[excited] Into the turbine hall! Keep it straight in there!",
    "dmflood_1": "[chuckles] The valley road's under water, folks! Boats only!",
    "dmdive_1": "[excited] Canopies sealed! Diving down the drowned valley!",
}
LINES.update(ROUND16)
# Round 17: laps on every course (lapnext / lapfinal), Stunt Show, the stunts (combo, bigcombo, shifttrick, wipeout) and the items
# (Race/Caller.cs: a person's racer firing one or hit by one)
ROUND17 = {
    "lapnext_1": "[excited] Round they go again, folks! Another lap!",
    "lapnext_2": "[showman] Next lap! Same course, fresh chances!",
    "lapnext_3": "[chuckles] Once more round, everybody! Nobody's tired yet!",
    "lapfinal_1": "[shouting] Final lap, folks! This is it!",
    "lapfinal_2": "[excited] Last time round! Give it everything!",
    "lapfinal_3": "[showman] The final lap! Somebody ring the bell!",
    "showstart_1": "[showman] Welcome to the Stunt Show, folks! Speed is nice, but style is everything today!",
    "showstart_2": "[excited] It's the Stunt Show! Flips, spins, rolls! Points for every one that lands!",
    "combo_1": "[excited] Ooh, a combo! Look at that!",
    "combo_2": "[showman] Spin and flip, folks! Lovely stuff!",
    "combo_3": "[delighted] Stuck the landing! The crowd loves it!",
    "combo_4": "[chuckles] Showing off, are we? I approve!",
    "bigcombo_1": "[shouting] What a combo! Are you seeing this?!",
    "bigcombo_2": "[excited] Trick after trick after trick! Incredible!",
    "bigcombo_3": "[showman] Ladies and gentlemen, that is a highlight reel!",
    "shifttrick_1": "[gasps] A trick while it changed shape! A shift trick!",
    "shifttrick_2": "[excited] Flipping and transforming at the same time! Bonus points!",
    "shifttrick_3": "[showman] Shift trick! I didn't know that was even allowed!",
    "wipeout_1": "[groans] Ooh! Landed on its roof. Those points are gone, folks.",
    "wipeout_2": "[sympathetic] Wipeout! Too much spin, not enough sky.",
    "wipeout_3": "[chuckles] Ooh, that landing was not in the brochure.",
    "ithit_1": "[gasps] Got hit! That's going to cost a second or two!",
    "ithit_2": "[chuckles] Ooh, right on target! Shake it off!",
    "ithit_3": "[excited] Direct hit, folks! Direct hit!",
    "itblock_1": "[impressed] Blocked it! Nice and safe!",
    "itblock_2": "[delighted] Bounced right off! Not today!",
    "itwrong_1": "[laughs] Wrong form! Wrong form! That's not a boat, that's a car! Or is it?",
    "itwrong_2": "[chuckles] Zapped into the wrong shape! That's going to be awkward!",
    "itbubble_1": "[delighted] Bubble trap! Up, up and away they go!",
    "itbubble_2": "[chuckles] Floating in a bubble, folks! Enjoy the view!",
    "itswap_1": "[gasps] They've swapped places! Just like that!",
    "itswap_2": "[excited] Swap spot! Now you're ahead, now you're not!",
    "itstorm_1": "[chuckles] A pocket storm! Somebody brought their own weather!",
    "itstorm_2": "[showman] Rain cloud incoming! Hope they packed a brolly!",
    "itegg_1": "[delighted] A dino egg! And it's hatched! Run, little raptor, run!",
    "itegg_2": "[excited] The baby raptor is off! It's chasing the leader!",
    "itrocket_1": "[excited] A firework rocket! Ooh! Ahh!",
    "itrocket_2": "[showman] Rocket away! Light up the sky, folks!",
    "itcannon_1": "[laughs] A human cannonball! There goes a clown!",
    "itcannon_2": "[excited] Fire the clown! Somebody always has to fire the clown!",
    "itflood_1": "[gasps] A mini flood! Surf's up, folks!",
    "itflood_2": "[excited] Here comes a wave! Everybody hold on!",
    "itbuck_1": "[shouting] It's Buck's Big Finish! Into the cannon and up the field!",
    "itbuck_2": "[showman] Buck's stunt cannon! From the back to the front in one boom!",
    "itrewind_1": "[confused] Hang on, didn't we just see that? A rewind, folks!",
    "ithorn_1": "[excited] The hype horn! Double points for every trick now!",
    "itmirage_1": "[amazed] Three of them? No, it's a mirage! Clever!",
    "itrainbow_1": "[delighted] A rainbow bridge! Off they go along it!",
}
LINES.update(ROUND17)
# his Tee'd Off clips that fit a race (copied as they are): id here -> Tee'd Off file
EXISTING = {
    "win_any_1": "winner_1", "win_any_2": "winner_3", "win_any_3": "winner_5", "win_any_4": "finale_1",
    "jump_4": "great_2", "miss_3": "ohno_1", "results_lost_1": "ohno_3",
}
EXISTING_WORDS = {
    "win_any_1": "Winner, winner! We have a winner!", "win_any_2": "Step right up and see the champion! Winner, winner!",
    "win_any_3": "Winner, winner! Somebody light the fireworks!", "win_any_4": "And the crowd goes WILD! Step right up and see a champion, folks!",
    "jump_4": "Stuck it! Did you see that, ladies and gentlemen?", "miss_3": "Ohhh! So close, folks! Try again, try again!",
    "results_lost_1": "Oops! Not a winner this time, but don't go away!",
}


def api_key():
    key = None
    if sys.platform == "win32":
        try:
            import winreg
            with winreg.OpenKey(winreg.HKEY_CURRENT_USER, "Environment") as h:
                key = winreg.QueryValueEx(h, "ELEVENLABS_API_KEY")[0]
        except OSError:
            key = None
    return key or os.environ.get("ELEVENLABS_API_KEY") or None


def to_ogg(mp3, path):
    tmp = path + ".mp3"
    with open(tmp, "wb") as f:
        f.write(mp3)
    # trim leading / trailing silence, a short fade at each end, level to -16 LUFS (Tee'd Off's voices and the music sit at -15 / -16),
    # mono 44.1 kHz Vorbis. Round 8: build 8's filter had "afade=t=out:st=0:d=0.02": a fade-out from the clip's first sample, so every line
    # recorded in Round 7 was silent after 20 ms (measured -43 LUFS): Dan's "can't hear most of Barnaby's lines". Those were re-recorded.
    af = ("silenceremove=start_periods=1:start_threshold=-45dB,areverse,silenceremove=start_periods=1:start_threshold=-45dB,"
          "afade=t=in:d=0.03,areverse,afade=t=in:d=0.005,loudnorm=I=-16:TP=-1.5:LRA=11,aresample=44100")
    subprocess.run([FFMPEG, "-y", "-loglevel", "error", "-i", tmp, "-af", af, "-ac", "1", "-ar", "44100", "-c:a", "libvorbis", "-q:a", "5", path], check=True)
    os.remove(tmp)


def copy_existing():
    n = 0
    for mine, theirs in EXISTING.items():
        src = os.path.join(TEEDOFF, theirs + ".ogg"); dst = os.path.join(OUT, mine + ".ogg")
        if os.path.exists(src) and not os.path.exists(dst):
            shutil.copyfile(src, dst); n += 1
    return n


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--go", action="store_true")
    ap.add_argument("--cap", type=int, default=12000)
    ap.add_argument("--redo", action="store_true", help="re-record every ElevenLabs line (build 8's silent clips)")
    args = ap.parse_args()
    if args.redo and not args.go:
        args.redo = True
    os.makedirs(OUT, exist_ok=True)
    todo = [(k, t) for k, t in LINES.items() if args.redo or not os.path.exists(os.path.join(OUT, k + ".ogg"))]
    est = sum(len(t) for _, t in todo)
    print(f"{len(LINES)} new lines, {len(todo)} not recorded yet (about {est} credits); {len(EXISTING)} Tee'd Off clips to copy")
    if not args.go:
        for k, t in todo:
            print(f"  {k}: {t}")
        print("dry run: add --go to record")
        return
    print(f"copied {copy_existing()} Tee'd Off clips")
    key = api_key()
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
        to_ogg(audio, os.path.join(OUT, k + ".ogg"))
        used = int(cost) if cost and cost.isdigit() else len(text); spent += used
        print(f"  {k}: {used} credits (total {spent})", flush=True)
    words = {k: t.split("] ", 1)[-1] for k, t in LINES.items()}
    words.update(EXISTING_WORDS)
    with open(os.path.join(OUT, "lines.json"), "w", encoding="utf-8") as f:
        json.dump({"speaker": "Barnaby", "lines": [{"id": k, "text": v} for k, v in sorted(words.items())]}, f, indent=1)
    print(f"done: {spent} credits; lines.json written")


if __name__ == "__main__":
    main()
