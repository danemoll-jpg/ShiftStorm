# Barnaby by name (Round 15 item 2)

Is "Barker" shown or spoken anywhere a player meets it? (`Tools/Audio/barnaby_audit.py`)

## 1. The built game's text

Searched: Builds\ShiftStorm\ShiftStorm_Data\Managed\Assembly-CSharp.dll (every string literal) and the shipped data files.

- Strings a player can see containing "Barker": **0**
- Strings containing "Barker" that are only log / check-report text or ids (never on screen): 3: `Audio/Voice/barker/lines`; `Audio/Voice/barker`; `barker`
- "Barker" in the shipped data files (subtitles, lines.json): **0**

## 2. What is spoken

384 voice clips (Barnaby, Buck, Abi, Rae, Chip, Vivi) transcribed with local speech-to-text (faster-whisper small.en).

- Clips whose written words say "Barker": **0**
- Clips in which "Barker" is heard: **0**

The Round 15 lines (and any clip that says "Barker"): written, heard, word match (1.00 = identical; under 0.80 flagged).

| who | id | written | heard | match |
|---|---|---|---|---|
| Barnaby | btcannon_1 | Ready... aim... BOOM! Human cannonball, folks! | Ready, aim, boom, human cannonball folks. | 1.00 |
| Barnaby | btcannon_2 | Fired out of the cannon! Through the hoops, out the top! | fired out of the cannon, through the hoops, out the top! | 1.00 |
| Barnaby | btclowns_1 | Clowns everywhere! How many fit in that little car? | Ha ha ha! Clowns everywhere! How many fit in that little car? | 0.86 |
| Barnaby | btelephants_1 | The elephants are giving everyone a shower! | The elephants are giving everyone a shower! | 1.00 |
| Barnaby | btfill_1 | What a show, folks! And nobody's even juggled yet! | What a show folks! And nobody's even juggled yet! | 1.00 |
| Barnaby | btfill_2 | Somebody tell the popcorn man to duck! | Hehe, somebody tell the popcorn man to duck! | 0.93 |
| Barnaby | btfill_3 | The greatest race under the sun! The tent's around here somewhere! | The greatest race under the sun! The tents around here somewhere! | 0.91 |
| Barnaby | btfill_4 | Hold on to your candy floss, folks, it's getting breezy! | Hold on to your candy floss, folks! It's getting breezy! | 1.00 |
| Barnaby | btglobe_1 | Into the Globe of Death! Round and round and out the other side! | into the globe of death round and round and out the other side | 1.00 |
| Barnaby | btglobe_2 | The Globe of Death, folks! Don't try this at home. Or anywhere! | The globe of death, folks! Don't try this at home or anywhere! | 1.00 |
| Barnaby | btjump_1 | Over the clown cars! Honk honk, folks! | Over the clown cars! Honk, honk, folks! | 1.00 |
| Barnaby | btpool_1 | Up out of the water and back to the midway! Here comes the finish! | Up out of the water and back to the Midway! Here comes the finish! | 1.00 |
| Barnaby | btrapids_1 | Ooh, through the rapids! Hold on to your hat! | Ooh! Through the rapids! Hold on to your hat! | 1.00 |
| Barnaby | btres_1 | And that's the show, folks! Give the racers a big top round of applause! | And that's the show, folks. Give the racers a big, top round of applause. | 1.00 |
| Barnaby | btres_2 | What a performance! Take a bow, every one of you! | What a performance! Take a bow, every one of you! | 1.00 |
| Barnaby | btseals_1 | Sea lions balancing balls! Now that's talent! | Hey, sea lions balancing balls. Now that's talent. | 0.93 |
| Barnaby | btsplash_1 | Splashdown! The elephants are jealous! | Hey, splashdown! The elephants are jealous! | 0.91 |
| Barnaby | btstart_1 | Ladies and gentlemen, children of all ages! Welcome to the Big Top Breakdown! | Ladies and gentlemen, children of all ages, welcome to the big top breakdown. | 1.00 |
| Barnaby | btstart_2 | Roll up, roll up! Four racers, one circus, and a cannon with their names on it! | Roll up, roll up! Four racers, one circus, and a cannon with their names on it! | 1.00 |
| Barnaby | btstart_3 | The Lustre Brothers Circus, folks! Mind the wagons, mind the clowns, and mind the elephants! | The Lustre Brothers Circus, folks! Mine the wagons, mine the clowns, and mine the elephants! | 0.80 |
| Barnaby | btsteamer_1 | There's the showboat, folks! Toot toot! | There's the showboat, folks! Toot toot! | 1.00 |
| Barnaby | btstrongman_1 | Look at the strongman, folks! He's holding up the whole midway! | Hey, look at the strong man folks. He's holding up the whole Midway | 0.83 |
| Barnaby | btsunken_1 | Through the sunken circus wagon! Mind the clowns' old shoes! | Through the sunken circus wagon, mind the clown's old shoes. | 0.90 |
| Barnaby | bttank_1 | The escape artist's tank! Is he still in there? | The escape artist tank! Is he still in there? | 0.89 |
| Barnaby | bttent_1 | Into the big top, folks! Once round the ring and into the cannon! | Into the big top, folks! Once round the ring, and into the cannon! | 1.00 |
| Barnaby | bttightrope_1 | Up on the tightrope! Don't look down, sweetheart! | Up on the tightrope! Don't look down, sweetheart! | 1.00 |
| Barnaby | bttrapeze_1 | The flying trapeze! They make it look so easy! | The flying trapeze! They make it look so easy! | 1.00 |
| Barnaby | btwagons_1 | Mind the wagon parade! They've got right of way! | Mind the wagon parade, they've got right of way! | 1.00 |
| Barnaby | btwings_1 | Wings out at the Clown Jump! Right over the Globe of Death! | Wings out at the clown jump, right over the globe of death! | 1.00 |
| Barnaby | btwings_2 | Look, no loop! Flying the long way round, folks! | Look! No loop! Flying the long way round, folks! | 1.00 |
| Buck | brief_bt_1 | Welcome to the Lustre Brothers Circus! Mind the wagons, they've got right of way. They've always had right of way. | Welcome to the Luster Brothers Circus. Mind the wagons, they've got right of way. They've always had right of way. | 0.95 |
| Buck | brief_bt_2 | Down the midway, round the ring, and out of the cannon. Don't worry. I asked about the cannon. They said it's mostly confetti. | down the midway, round the ring, and out of the cannon. Don't worry. I asked about the cannon. They said it's mostly confetti. | 1.00 |
| Buck | brief_bt_3 | The top flap blew open this morning, so that's your way out of the tent. Lionel calls it a feature. I call it a draught. | The top flap blew open this morning, so that's your way out of the tent. Lionel calls it a feature. I call it a draft. | 0.96 |
| Buck | ch_c3_intro_1 | Vivi says the cannon is her stage. From the big top's door to the top flap, one on one. She's already practising her bow. | Vivi says the cannon is her stage. From the big top's door to the top flap, one-on-one. She's already practicing her bow. | 0.96 |
| Buck | res_bt_back_1 | Not this time, but the show's not over. Lionel's already resetting the ring. Shall we go again? | Not this time! But the show's not over! Lionel's already resetting the ring. Shall we go again? | 1.00 |
| Buck | res_bt_back_2 | The cannon takes a little getting used to. My first time, I landed in the popcorn stand. Very tasty. | Heh, the cannon takes a little getting used to. My first time, I landed in the popcorn stand. Very tasty! | 0.97 |
| Buck | res_bt_first_1 | First place under the big top! Lionel's ringing his bell. Through gritted teeth, but he's ringing it. | First place under the big top, Lionel's ringing his bell, through gritted teeth, but he's ringing it. | 1.00 |
| Buck | res_bt_first_2 | A winner! Wagons, the Globe, the cannon and the river. Somebody get that racer a top hat. | A winner! Wagons, the globe, the cannon, and the river! Somebody get that racer a top hat! | 1.00 |
| Buck | res_bt_podium_1 | On the podium! The elephants approve. One of them sneezed, and I'm taking that as a yes. | On the podium! The elephants approve. One of them sneezed, and I'm taking that as a yes. | 1.00 |
| Buck | res_bt_podium_2 | Lovely racing! You came out of that cannon like a real pro. Better than me in eighty-seven. | Lovely racing! You came out of that cannon like a real pro! Better than me in 87! | 0.91 |
| Buck | short_bt_1 | Midway, big top, cannon, river. Have a ball, partner! | Midway, Big Top, Cannon, River, have a ball, partner! | 1.00 |
| Buck | short_bt_2 | Mind the wagons and aim for the top flap! | Mind the wagons and aim for the top flap! | 1.00 |
| Buck | tour_done_1 | Three stops, three shows, and a tour that's really happening. Thank you, partner. Now, about that volcano... | Three stops, three shows, and a tour that's really happening. Thank you, partner. Now, about that volcano. | 1.00 |
| Buck | tour_s3_intro_1 | The Lustre Brothers Circus. Lionel Lustre and I shared a bill back in eighty-seven. He had elephants. I had a bus. | The Lustre Brothers Circus. Lionel Lustre and I shared a bill back in 87. He had elephants, I had a bus. | 0.93 |
| Buck | tour_s3_intro_2 | He's sent a letter. Let me read it. Quote: Buck, old boy, I hear you've gone respectable. Prove you've still got it. Unquote. | He's sent a letter, let me read it. Quote, buck old boy, I hear you've gone respectable. Prove you've still got it. Unquote. | 1.00 |
| Buck | tour_s3_intro_3 | So we race his circus! The midway, the Globe of Death, the ring, and the human cannonball, straight out of the top flap. | So we race his circus, the Midway, the Globe of Death, the Ring, and the Human Cannonball straight out of the top flap. | 1.00 |
| Buck | tour_s3_intro_4 | A top three finish, and I get to send Lionel a letter of my own. I've already written it. It's very short. | A top three finish, and I get to send Lionel a letter of my own. I've already written it. It's very short. | 1.00 |
| Buck | tour_s3_pass_1 | Ha! Did you see Lionel's face? I'm framing it. Well, I'm framing the photo. He wouldn't sit still. | Ha! Did you see Lionel's face? I'm framing it. Well, I'm framing the photo. He wouldn't sit still. | 1.00 |
| Buck | tour_s3_pass_2 | He shook my hand, you know. Said we still have it. He said we. I'm choosing to hear it that way. | He shook my hand, you know, said, we still have it. He said, we. I'm choosing to hear it that way. | 1.00 |
| Buck | tour_s3_retry_1 | Not quite the top three. Lionel's being gracious. Very loudly gracious. Let's give him something to be quiet about. | Not quite the top three. Lionel's being gracious. Very loudly gracious. Let's give him something to be quiet about. | 1.00 |
| Buck | tour_s3_retry_2 | Keep to the middle of the midway, the wagons keep to the sides. And trust the cannon. I mostly do. | Keep to the middle of the midway, the wagons keep to the sides, and trust the cannon. I mostly do. | 1.00 |
| Rae | chintro_1 | One on one, rookie? Try to keep my bubbles in sight! | One-on-one rookie, try to keep my bubbles in sight. | 1.00 |
| Rae | chlose_1 | Okay. Okay! You earned that one. | Okay, okay, you earned that one. | 1.00 |
| Rae | chwin_1 | Ha! The waterfall is MY turf! | Ha! The waterfall is my turn! | 0.83 |
| Chip | chintro_1 | Ladies and gentlemen, the Showtime coaster special! Try to keep up! | Ladies and gentlemen, the Showtime Coaster Special. Try to keep up! | 1.00 |
| Chip | chlose_1 | You beat me on my own coaster? That's going in YOUR highlight reel. | You beat me on my own coaster? That's going in your highlight reel. | 1.00 |
| Chip | chwin_1 | And THAT, folks, is how you ride a coaster! | And that, folks, is how you ride a coaster! | 1.00 |
| Vivi | chintro_1 | Darling, the cannon is my stage. Try not to get lost in my spotlight! | Darling, the cannon is my stage. Try not to get lost in my spotlight. | 1.00 |
| Vivi | chlose_1 | You stole my spotlight! [sighs] Fine. Take a bow, darling. A small one. | Ah! You stole my spotlight! Ah, fine. Take a bow, darling. A small one. | 0.89 |
| Vivi | chwin_1 | And the crowd goes wild! For me, darling. Obviously. | And the crowd goes wild, for me darling, obviously. | 1.00 |

**PASS**: no "Barker" shown or spoken to the player.
