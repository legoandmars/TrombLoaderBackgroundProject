# TrombLoader Background Project
A Unity Project used to create custom [Trombone Champ](https://store.steampowered.com/app/1059990/Trombone_Champ/) chart backgrounds.

Creating a custom background is relatively easy to get into, especially if you already have existing Unity Editor experience.

Because chart backgrounds use AssetBundles under the hood, you can use nearly every Unity Component. A major exception is that you cannot put custom scripts, but this project includes some helper/event scripts to make writing code less necessary.

# Usage/Tutorial
Make sure you already have a Trombone Champ song file ready to go! More info can be found at the [Midi2TromboneChamp](https://github.com/NyxTheShield/Midi2TromboneChamp) wiki.


First, download `Unity 2019.3.15f1` from the [Unity Archive page](https://unity3d.com/get-unity/download/archive). Make sure to get 2019.3.15f1, as going to a later/earlier version could make the game unable to load your background. Using Unity Hub is higly recommended!

Download the latest published project from the [releases tab](https://github.com/legoandmars/TrombLoaderBackgroundProject/releases/latest), unzip it, and open it up in Unity.

## Creating a Backgronud
Once you get into the editor, you should see an `Example` scene. This contains a background template with everything you need.

Please note that because of the way the game is structured, moving the `Template`, `Background`, or `Foreground` objects out of order could result in your background breaking. Renaming is fine - it's recommended to rename the [root object](####Root) to your song name.

#### Root
The root of the template is named `Template`, and contains both the Camera that will render your background, and the Event Manager that you can use to hook up certain in-game events - more detail can be found in the [Events section](##Events).

The Camera can be modified however you want - changing the perspective, FOV, rotation, etc. Just make sure it stays on the root object!

If you want to render OVER the [trombone player ingame](https://user-images.githubusercontent.com/34404266/193167408-35ba4db9-bac1-460c-89fc-cd1944646102.png), raise the `Depth` setting to `10` instead of `-10`.


#### Background
Inside of the root object, you can find a `Background` object. This object has a `BackgroundImage` inside of it, which you can swap it with your own image.

To swap it out, drag your background image into the Unity Project's `Assets` window, and change `Texture Type` on the right window from `Default` to `Sprite (2D and UI)`.

Please note that because of the way Unity sprites work, the file's resolution is directly tied to the physical size. If you want it to be the same size as the Trombone Champ background, use a `1920x1080` image, or rescale it in the editor to be the same physical size as a 1080p image.

#### Foreground
This object is also found inside of the root object, and is largely the same as the `Background` object.

If you want things to render on top of the background, such as particles, moving image effects, 3d geometry, etc, put it in this layer.

## Events
Hooking your background up to react to in-game occurrences is a very important step in bringing it to life.

Unity Events can be used to do a wide variety of things, namely:
- Starting Animations on an Animator
- Enabling/Disabling a GameObject using SetActive
- Spawning Particles on a ParticleSystem

Animations can be especially powerful because they allow you to control multiple things at once. For example, you could hook up an animation that disables the main background image, enables a second background image, and starts spawning fire for the next 20 seconds.

The root object has a `TromboneEventManager` on it, which contains the following events:

#### Normal Events
- OnBeat: Triggered on every song beat
- OnBar Triggered on every song bar/measure
- NoteStart: Triggered when a note starts, regardless of if it's hit or not
- NoteEnd: Triggered when a note ends, regardless of if it was hit or not


#### Special Events
- Combo Updated: Triggered whenever the combo is changed (raised from a hit, or lowered from a miss)
     - Since this is an `int` event, it can be used to trigger certain int-specific methods, such as spawning exactly `x` particles, or displaying `x` in text using the `TextEventHandler` helper.
- Mouse Position Updated: Triggered whenever the mouse is moved. Bottom left of the scren is (0, 0, 0), top right is (1, 1, 0)
     - Since this is a `Vector3` event, it can be used to trigger certain vector-specific methods, such as moving a cube's local position to `x, y, z` or making a cube look at `x, y, z`.


If you want to hook something up to one of these events, simply click on the `+` icon to add a new event. 

Next, Drag the object you want to modify with the event onto the `None (Object)` field.

Now you can choose the component and method you want to modify. This depends on what you want to do - if you want to enable particles, you could select the `ParticleSystem` component, and then the `Play` method, for example.

If you're using a special event, you'll have two sets of methods to choose from. The one on the top is `Dynamic`, and will use the value that's passed through the event, such as the Mouse Position. The one of the bottom is `Static`, and will trigger the same thing regardless of what the event's value is. [Image Example](https://user-images.githubusercontent.com/34404266/193170298-b4826ffd-d349-42c4-a862-2c966603e385.png)

#### An Important Note
Although there are a lot of specific methods you can use to trigger things, such as `ParticleSystem.Play()`, try to prioritize trying to use `GameObject.SetActive()` whenever possible to help make effects reusable.

For example, if you have a particle system, you could make it play on start (the default) and simply call `SetActive` to enable it.

Animations are also extremely helpful for reducing the complexity of your UnityEvents, and moving logic into something that's much easier to control in-editor.

#### Song Events
If you want to have an event at a specific point in a song, the best way to accomplish it is to use a `Background Event`. The ability to manually define an event at any point in a song is super useful for song-synced things such as background changes.

To add a new background event handler, click the `Add Background Event` button at the bottom of the root's `TromboneEventManager`.
Once created, you can set a background event ID. Make sure you use a **UNIQUE ID** for each background event to avoid syncing up with the wrong parts of a song!


#### Adding the events to the chart
After setting up a background event, you still have to define at which point in song it'll trigger. At the time of writing, since this is a relatively new feature, editors don't quite support it, and you'll have to do a bit of manual JSON editing.

Open up your `song.tmb` file in your favorite text editor, and hit ctrl+f to see if you have a `bgdata` array.

If you don't have one, you'll have to add one to the end of the file like this.

```
... "savednotespacing": 120, "endpoint": 643, "timesig": 2, "tempo": 100, "UNK1": 0} 
// Before ^
... "savednotespacing": 120, "endpoint": 643, "timesig": 2, "tempo": 100, "UNK1": 0, "bgdata": []} 
// After ^
```

Next, you'll need to create the event. Thankfully, the format is relatively simple.
```
[TimeInSeconds, EventID, TimeInBeats]
// First number is always just the time in seconds
// Second number is the Event ID you entered in the Unity Editor
// Third number follows the simple formula: (60 / bpm * TimeInSeconds)

// For example, an event of ID 1, twenty seconds into a 100BPM song
[20.0, 1, 12.0]

// An event of ID 2, thirty and a half seconds into a 100BPM song
[30.5, 2, 18.3]
```

There's very few limitations on what you can do - you can create multiple events with the same ID if you want to trigger it multiple times.

Once you've put together your events, you need to put them into `bgdata`, seperated by a comma.
```
... "savednotespacing": 120, "endpoint": 643, "timesig": 2, "tempo": 100, "UNK1": 0, "bgdata": []} 
// Before ^
... "savednotespacing": 120, "endpoint": 643, "timesig": 2, "tempo": 100, "UNK1": 0, "bgdata": [[20.0, 1, 12.0], [30.5, 2, 18.3]]} 
// After ^
```

## Exporting
Once you're finished with your background and want to try it ingame:
- Make sure that everything you want to include is inside of the `Background`/`Foreground` objects
- Click on the root object
- Go to the Camera in the inspector and press `Export for TrombLoader`

Export the `bg.trombackground` file into your song's folder and you're done!

## Extras & Tips

Since you have access to nearly every Unity component, what you do is largely going to be limited by how creative you are with existing components. Here's a few things that could be useful to play around with:
- Unity Constraints
- Writing Shaders / Importing other people's shaders
- Particle Systems
- RigidBody physics
- Cameras hooked up to RenderTextures

If you have any general questions, or questions on how to do something, please feel free to join the [Trombone Champ Modding discord](https://discord.gg/KVzKRsbetJ).

#### Background Movement

If you want to give a bit more movement to your `Background` object without animating anything, the trombone champ mod comes with a few preset background movement options.

- `none`: No background movement.
- `spiral`: The background image moves around in a spiral formation.
- `spiralslower`: A slower version of `spiral`.
- `spiralslowest` A very slow version of `spiral`.
- `bounceslow` The background image slowly oscillates slightly up and down.
- `bouncefast` The background image bounces around in the shape of a square.'

If you want to use one of these background movement options, add a backgroundMovement field to your song.tmb, similar to the `Adding the events to the chart` section.

```json
... "savednotespacing": 120, "endpoint": 643, "timesig": 2, "tempo": 100, "UNK1": 0, "bgdata": [...]} 
// Before ^
... "savednotespacing": 120, "endpoint": 643, "timesig": 2, "tempo": 100, "UNK1": 0, "bgdata": [...], "backgroundMovement": "bouncefast"} 
// After ^
```
