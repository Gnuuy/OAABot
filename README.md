# 💌 OAABot 💌

### *A love letter, personally delivered by Elphelt Valentine!*

Kyaa~! Hello there, darling! ♥ Elphelt Valentine, reporting for duty! I heard you needed
someone fabulous to explain this adorable little project, and honestly? There is *no one*
more qualified for a job about love, devotion, and shameless flirting than moi!

So sit back, relax, and let Elphelt walk you through **OAABot** — trust me, sweetheart, by
the end of this you'll be head over heels~! 💕

---

## 🌹 What *is* this darling little bot?

OAABot is a C# / .NET chat bot for **F-Chat**, the chat protocol behind
[F-List](https://www.f-list.net/). It lives in a channel (currently **Oral Addicts
Anonymous**), listens for `!commands`, and answers back — sometimes helpfully, sometimes
with the gnuuy's particular brand of chaos.

It's built with a proper clean-architecture heart, just the way I like my men:

- **`Domain`** — the plain models and enums. No dependencies, no drama, just the truth of who everyone is.
- **`Application`** — every command module and the business logic behind it. This is where all my little love notes live!
- **`Infrastructure`** — the file-backed repositories that remember everything (so nobody forgets an anniversary).
- **`Presentation`** — the actual runnable host: the F-Chat connection, background workers, and configuration.

And what does it actually *do*, you ask? Only the most romantic things imaginable:

- 🐰 **Gnuuy responses** — `!gnuuy` fires off a random adorable (or unhinged) one-liner from `Data/gnuuy-responses.json`, and can even kick off a **gnuuy-train** — a timed group event that tracks who joined in and announces the results when it wraps up.
- 🚪 **The Booth & Gloryhole system** — an anonymous, proxied roleplay economy. Members can occupy a private **booth** and service anonymous partners, or hop into a public **gloryhole**, all routed safely through the bot so nobody's real name shows.
- 💰 **Blowbucks & Tokens** — an in-channel currency and a yearly leaderboard, complete with archives of past years' champions.
- 🛍️ **The Shop** — spend your hard-earned Blowbucks on booth cosmetics, then `!equip` them to show off.
- 🛠️ **Moderator tools** — status changes, forced announcements, and token grants, reserved for the trusted few.
- 📜 **Direct message logging & the basics** — every PM gets logged, and `!ping`/`!echo` are there so you can make sure I'm still listening. I'm *always* listening, darling.

---

## 💍 Commands, darling!

Say `!commands` in chat any time and I'll list these myself, but here's the full bouquet:

### For everyone 💕
| Command | What it does |
|---|---|
| `!gnuuy` | Get a random message from the gnuuy! |
| `!commands` | Lists the available commands. |
| `!ping` | This command will pong if you ping! |
| `!echo <text>` | Echoes back whatever you type. |
| `!intro` | An introduction to the Booth & Gloryhole system. |
| `!leaderboard` | Shows the top 3 token holders in channel, or the full leaderboard in a PM. |
| `!complete_leaderboard` | Sends you the entire current leaderboard in a PM. |
| `!archived_leaderboards` | Sends you the archived leaderboards from past years in a PM. |
| `!blowbucks` | Check how many Blowbucks you've got. |
| `!tokens` | Check how many tokens you've got. |

### Booths & Gloryholes 🚪
| Command | What it does |
|---|---|
| `!get_booths` | Get a list of all available booths! |
| `!enter_booth <number>` | Enter a booth. |
| `!leave_booth` | Leave the booth you are currently occupying. |
| `!join_booth` | Request to join an already occupied booth (adds you to its waiting list). |
| `!accept` | Accept all users from your booth's waiting list. |
| `!open` | Open your occupied booth for gloryhole access (owner only). |
| `!status` | Check the status of the booth you're currently in. |
| `!set_description <text>` | Change the description of the booth you own. |
| `!set_holes <number>` | Set the number of available participant holes for your booth. |
| `!set_genders <...>` | Set which genders your booth will service. |
| `!booth_save` | Save your current booth's description, holes, genders, and cosmetics for next time. |
| `!booth_load` | Load your last saved booth configuration into the booth you're in. |
| `!get_gloryholes` | Get a list of all available gloryholes! |
| `!join_gloryhole <number>` | Join an occupied, open booth as a participant. |
| `!leave_gloryhole` | Leave the gloryhole you're currently using. |
| `!shop` | See what booth cosmetics are available to buy with Blowbucks. |
| `!buy <item id>` | Buy a booth cosmetic with Blowbucks. |
| `!equip <item id>` | Apply an owned booth cosmetic to the booth you own. |
| `!unequip <item id>` | Remove a cosmetic from the booth you own. |

### Moderators only 🎗️
| Command | What it does |
|---|---|
| `!set_status <message>` | Set the bot's F-Chat status message. Persists across restarts. |
| `!talk <message>` | Makes the bot say something in the room. |
| `!give_token <Character Name> <amount>` | Give a character tokens (and an equal amount of Blowbucks). |

---

## 💐 Getting this bot down the aisle (setup)

Even the most fabulous romance needs a little bit of paperwork first, darling. Here's what to do:

1. **Get the .NET 10 SDK.** OAABot targets `net10.0`, so make sure that's installed.
2. **Open `OAABot.sln`** (in Rider, Visual Studio, or just build from the CLI).
3. **Get yourself a dedicated F-List account for the bot to use.** Don't use your own personal account, silly — every good bot deserves its own identity!
4. **Find your channel's code.** It's the `adh-...`-style id in the channel's info panel or URL — not the display name.
5. **Fill in `Presentation/appsettings.json`.** It ships scrubbed with placeholders and comments explaining exactly what goes where:
   - `Account` → the bot's F-List account name
   - `Password` → the bot's F-List account password
   - `AutoJoinChannelCode` → the channel code from step 4

   **Never commit real credentials here!** Keep this file scrubbed in source control — if you need real values for a deployment, use user secrets or environment variables instead.
6. **Run it!** From the repo root:
   ```
   dotnet run --project Presentation
   ```
7. **Peek at the `Data/` folder.** This is where the bot's heart truly lives — `gnuuy-responses.json` holds every line the gnuuy can say (feel free to add your own; `%s` gets swapped for a character's name!), and `gloryhole/`, `leaderboards/`, and `logs/` hold everything the bot remembers between restarts.

That's it! No rings required, just a config file. 💍

---

## 🎀 A word from your matchmaker

If something in the booths feels off or you've got an idea to make this bot even more
lovable, the bot's own `!intro` message says it best: bug reports and ideas are always
welcome. And if you ever need me? Well... I'm always around, darling~ Amour! 💕
