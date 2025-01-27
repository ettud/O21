namespace O21.Game.U95

open System
open System.IO
open System.Threading.Tasks
open Microsoft.FSharp.NativeInterop

open Raylib_CsLo
open type Raylib_CsLo.Raylib

open O21.Game
open O21.Game.U95.Fish
open O21.Resources

#nowarn "9"

type Sprites = {
    Bricks: Map<int, Texture>
    Background: Texture[]
    Fishes: Fish[]
    HUD: HUDSprites
    Bonuses: BonusSprites
}
    with
        interface IDisposable with
            member this.Dispose() =
                for t in this.Bricks.Values do
                    UnloadTexture(t)
                for t in this.Background do
                    UnloadTexture(t)

module Sprites =

    let private transparentColor = struct(0xFFuy, 0xFFuy, 0xFFuy)
    let private isColor(struct(r1, g1, b1), struct(r2, g2, b2)) =
        r1 = r2 && g1 = g2 && b1 = b2

    let private createSprite (colors: Dib) (transparency: Dib) =
        let width = colors.Width
        let height = colors.Height
        let image = GenImageColor(width, height, BLANK)
        let colors = Array.init (width * height) (fun i ->
            let x = i % width
            let y = i / width
            let isTransparent = isColor(transparency.GetPixel(x, y), transparentColor)
            if isTransparent then
                BLANK
            else
                let struct(r, g, b) = colors.GetPixel(x, y)
                Color(r, g, b, 255uy)
        )
        use colorsPtr = fixed colors
        let image = Image(
            data = NativePtr.toVoidPtr colorsPtr,
            width = width,
            height = height,
            format = int PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8,
            mipmaps = 1
        )
        let texture = LoadTextureFromImage(image)
        texture

    let CreateSprite (colors: Dib) =
        let width = colors.Width
        let height = colors.Height
        let colors = Array.init (width * height) (fun i ->
            let x = i % width
            let y = i / width
            let struct(r, g, b) = colors.GetPixel(x, y)
            Color(r, g, b, 255uy)
        )
        use colorsPtr = fixed colors
        let image = Image(
            data = NativePtr.toVoidPtr colorsPtr,
            width = width,
            height = height,
            format = int PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8,
            mipmaps = 1
        )
        let texture = LoadTextureFromImage(image)
        texture
       
    let private loadBonuses (exeGraphics: Dib[]): BonusSprites =
        let lifebuoyMasks = [|172..183|]
        let lifebuoyTextures = [|160..171|]
        let bonusesMasks = [|202; 203; 205; 206; 207; 208; 209; 210; 211; 212|]
        let bonusesTextures = [|213; 214; 216; 217; 218; 219; 220; 221; 222; 223|]
        {
            Lifebuoy = Array.init 12 (fun i ->
                createSprite exeGraphics[lifebuoyTextures[i]] exeGraphics[lifebuoyMasks[i]]
            )
            Static = Array.init 10 (fun i ->
                createSprite exeGraphics[bonusesTextures[i]] exeGraphics[bonusesMasks[i]]    
            )
            LifeBonus = createSprite exeGraphics[215] exeGraphics[204] 
        }
             
    let private loadBricks (brickGraphics: Dib[]) =
        seq { 1..9 }
        |> Seq.map(fun direction ->
            let colors = brickGraphics[direction]
            let transparency = brickGraphics[direction + 10]
            direction, createSprite colors transparency
        ) |> Map.ofSeq
    
    let private loadBackgrounds (backgroundGraphics: Dib[]): Texture[] =
        Array.init backgroundGraphics.Length (fun i ->
            CreateSprite backgroundGraphics[i]
        )
        
    let private createFish (index: int) (fishGraphics: Dib[]): Fish =
        let shift = index * 9
        {
            Width = fishGraphics[index * 9].Width
            Height = fishGraphics[index * 9].Height
            
            LeftDirection = Array.init 8 (fun i ->
                createSprite fishGraphics[shift + i + 45] fishGraphics[shift + i]
            )
            
            RightDirection = Array.init 8 (fun i ->
                createSprite fishGraphics[shift + i + 135] fishGraphics[shift + i + 90] 
            )
            
            OnDying = [|
                  createSprite fishGraphics[shift + 53] fishGraphics[shift + 8];
                  createSprite fishGraphics[shift + 143] fishGraphics[shift + 98]
                  |]
        }
               
    let private loadFishes (fishGraphics: Dib[]): Fish[] =
        Array.init (fishGraphics.Length / 36) ( fun i ->
            createFish i fishGraphics
        )
        
    let private loadHUD (exeGraphics: Dib[]): HUDSprites =
       let hud = [| 26; 27; 29; 185; 159;|]
       let controls = [| 5; 25; 28; 184; 224; |]
       let abilities = [| 186..191 |]
       let digits = [| 192..201 |]
       {
           Abilities = Array.init abilities.Length (fun i ->
               CreateSprite exeGraphics[abilities[i]]
           )
           HUDElements = Array.init hud.Length (fun i ->
               CreateSprite exeGraphics[hud[i]]
           )
           Digits = Array.init digits.Length (fun i ->
               CreateSprite exeGraphics[digits[i]]    
           )
           Controls = Array.init controls.Length (fun i ->
               CreateSprite exeGraphics[controls[i]]    
           )
       }

    let LoadFrom (directory: string): Task<Sprites> = task {
        let brickResources = Graphics.Load(Path.Combine(directory, "U95_BRIC.DLL"))
        
        let fishes = Graphics.Load(Path.Combine(directory, "U95_PIC.DLL"))
        
        let exeSprites = Graphics.Load(Path.Combine(directory, "U95.EXE"))
        
        let backgrounds = Background.LoadBackgrounds(directory)
        
        return {
            Bricks = loadBricks brickResources
            Background = loadBackgrounds backgrounds
            Fishes = loadFishes fishes
            HUD = loadHUD exeSprites
            Bonuses = loadBonuses exeSprites
        }
    }
