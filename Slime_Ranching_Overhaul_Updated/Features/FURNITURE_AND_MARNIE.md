# Hutch enrichment furniture and Marnie's shop

**Vendor:** Marnie. Use a dedicated shop stock group, with prices and progression unlocks configurable. Exact prices TBD.

| Furniture | Animation/behavior | Gameplay impact |
|---|---|---|
| Slime Trampoline | Bounce, compression, jump arc, landing; tiny higher, giant compresses more | Mostly decorative, small happiness |
| Slime Playground | Play and interact in groups | Happiness/social |
| Heated Slime Stone | Huddle/rest/sleep, especially in winter | Winter comfort |
| Gel Pond | Swim, splash, juveniles at edges | Happiness; rare existing-item drops |
| Compost Trough | Consume fishing trash, algae, monster drops | Faster juvenile maturation |
| Slime Bed | Curl up and sleep | Decorative |
| Slime Slide | Climb and slide | Decorative |
| Slime Tunnel | Crawl through transparent tube | Decorative |
| Slime Swing | Sit and rock | Decorative |
| Slime Lamp | Gather around colored light | Decorative |
| Slime Cushion | Squish and spring back | Decorative |
| Slime Ball Pit | Dip into colorful balls, pop out | Decorative |
| fairy lights | makes it slightly brighter and turned on/off depending on time of day | Decorative |
water bowls and feeding troughs 

make it so you can apply your own wallpaper and flooring like the normal house

## Prototype status

The Slime Playground is implemented as group-play furniture: it reserves one placed object for two nearby Ranch Slimes and starts their short hop animations together. It has no happiness or friendship effect yet. Marnie's default prototype price is 6,000g, configurable in `config.json`; price and progression remain open for review.

## Interaction architecture
Furniture exposes a target position, eligible sizes, interaction duration, maximum concurrent users, animation, cooldown and optional happiness delta. A hutch-level scheduler periodically assigns idle slimes to available furniture. Use visual offsets for bouncing rather than moving collision boxes vertically. Reserve targets to avoid multiple slimes deadlocking. Skip or simplify offscreen animations.

**Rule:** Do not make decorative objects mandatory for optimal breeding; favor expressiveness and player customization.
