﻿module Geometry

open System

/// Represents a vector in 2D space.
type [<Struct>] Vector2 (x : float, y : float) =
    /// The X component of this vector.
    member this.X = x
    /// The Y component of this vector.
    member this.Y = y

    /// Returns the result of multiplying a vector by a scalar.
    static member (*) (v : Vector2, k : float) = Vector2 (v.X * k, v.Y * k)
    static member (*) (k : float, v : Vector2) = Vector2 (v.X * k, v.Y * k)
    static member (/) (v : Vector2, k : float) = Vector2 (v.X / k, v.Y / k)
    /// Returns the dot product of two vectors.
    static member (*) (a : Vector2, b : Vector2) = a.X * b.X + a.Y * b.Y
    /// Returns the result of adding two vectors.
    static member (+) (a : Vector2, b : Vector2) = Vector2 (a.X + b.X, a.Y + b.Y)
    /// Returns the result of subtracting one vector by another.
    static member (-) (a : Vector2, b : Vector2) = a + (b * - 1.0)

    /// The squared magnitude of this vector.
    member this.SqLength = this * this

    /// The magnitude of this vector.
    member this.Length = sqrt this.SqLength

    static member Zero = Vector2 (0.0, 0.0)

    override this.ToString () =  "(" + string this.X + ", " + string this.Y + ")"

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Vector2 =
    /// The zero vector
    let zero = Vector2.Zero

    let x (a : Vector2) = a.X

    let y (a : Vector2) = a.Y

    /// Returns the distance between two vectors
    let dist (a : Vector2) (b : Vector2) = (a - b).Length

    /// Normalizes a given vector
    let norm (a : Vector2) = a / a.Length
    
    /// Returns the two normals of a vector as the tuple (leftNorm, rightNorm)
    let normals (v : Vector2) = (Vector2 (v.Y, -v.X), Vector2 (-v.Y, v.X))

    /// Returns the projection of a vector onto another vector
    let proj (a : Vector2) (b : Vector2) = a * ((a * b) / (a.Length * b.Length))

    /// Calculates the angle between two vectors in radians.
    let angle (a : Vector2) (b : Vector2) = acos ((a * b) / (a.Length * b.Length))

    /// Takes the sign of the 2D cross product of two vectors (ie. the z component of the cross product of these vectors in 3-space.
    /// If the sign is positive, the angle between the two vectors can be thought of as a counter-clockwise turn
    /// If the sign is negative, the angle is a clockwise turn.
    /// If the sign is zero, the points are colinear.
    let crossSign (a : Vector2) (b : Vector2) = sign (a.X * b.Y - a.Y * b.X)
    
/// Represents a point in 2D space.
type Point2 = Vector2

/// An affine transformation in 2D space.
type [<Struct>] Transform (offset : Point2, x : Vector2, y : Vector2) =
    /// Gets the x component of this transform (the amount that the horizontal component is multiplied by when transformed).
    member this.X = x

    /// Gets the y component of this transform (the amount that the vertical component is multiplied by when transformed).
    member this.Y = y

    /// Gets the offset of this transform (the position of the origin when transformed).
    member this.Offset = offset

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Transform =
    /// The identity transform.
    let identity = Transform (Point2.Zero, Vector2 (1.0, 0.0), Vector2 (0.0, 1.0))

    /// Applies a given transform to a given point.
    let apply (p : Point2) (t : Transform) = t.Offset + (t.X * p.X) + (t.Y * p.Y)

    /// Applies a given transform to a given vector.
    let applyVector (v : Vector2) (t : Transform) = (t.X * v.X) + (t.Y * v.Y)

    /// Composes two transforms to be applied in the order they are given.
    let compose (a : Transform) (b : Transform) =
        Transform (b |> apply a.Offset, b |> applyVector a.X, b |> applyVector a.Y)

    /// Creates a rotation transform for a certain angle in radians.
    let rotate (theta : float) = 
        Transform (Point2.Zero, Vector2 (cos theta, sin theta), Vector2 (-(sin theta), cos theta))

    /// Creates a scale transform with the given scale factor.
    let scale (amount : float) =
        Transform (Point2.Zero, Vector2 (amount, 0.0), Vector2 (0.0, amount))

    /// Creates a scale transform with independant scale factors for each axis.
    let scaleIndependant (horizontal : float) (vertical : float) =
        Transform (Point2.Zero, Vector2 (horizontal, 0.0), Vector2 (0.0, vertical))

    /// Creates a shear transform along the y axis with the given angle in radians.
    let shear (omega : float) = 
        Transform (Point2.Zero, Vector2 (1.0, 0.0), Vector2 (omega, 1.0))

    /// Creates a scale transform with a given offset
    let translate (offset : Point2) =
        Transform (offset, Vector2 (1.0, 0.0), Vector2 (0.0, 1.0))

    /// Gets the determinant of a transform.
    let determinant (t : Transform) = 
        let x = t.X
        let y = t.Y
        (x.X * y.Y) - (x.Y * y.X)

    /// Gets the inverse of a transform.
    let inverse (t : Transform) =
        let x = t.X
        let y = t.Y
        let offset = t.Offset
        let idet = 1.0 / (determinant t)
        Transform (
            Point2 ((y.Y * offset.X - y.X * offset.Y) * -idet, 
                   (x.Y * offset.X - x.X * offset.Y) * idet),
            Vector2 (y.Y * idet, x.Y * -idet),
            Vector2 (y.X * -idet, x.X * idet))

    /// Normalizes this given transform so that there is no stretching or skewing when applied 
    /// as a projection transform to a viewport of the given aspect ratio.
    let normalize (aspectRatio : float) (t : Transform) =
        if aspectRatio < 1.0 then compose (scaleIndependant aspectRatio 1.0) t
        else compose (scaleIndependant 1.0 (1.0 / aspectRatio)) t

/// Represents a non-axis-aligned rectangle in 2D space
type Rectangle (center : Point2, width : float, height : float, theta : float) =
    let mutable center = center
    let mutable theta = theta

    // Constructs a rectangle using the positions of its corners.
    new (topLeft : Point2, topRight : Point2, bottomLeft : Point2, bottomRight : Point2) =
        let hDiff = topRight - topLeft
        let vDiff = topLeft - bottomLeft
        let theta = atan2 hDiff.Y hDiff.X
        let width = hDiff.Length
        let height = vDiff.Length
        let center = 
            let offset = 0.5 * Point2 (width, height)
            (Transform.translate (offset)) |> Transform.apply bottomLeft
        Rectangle (center, width, height, theta)

    /// The full width of this rectangle
    member this.Width = width
    
    /// The full height of this rectangle
    member this.Height = height

    /// The center point of this rectangle
    member this.Center 
        with get () = center
        and set value = center <- value

    /// The angle by which this rectangle is rotated about its center in radians.
    member this.Theta 
        with get () = theta
        and set value = theta <- value
            

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Rectangle =
    /// Gets the (absolute) location of the top-left corner of a rectangle.
    let inline topLeft (r : Rectangle) =
        let rotationTransform = Transform.rotate (r.Theta)
        let offset = Point2 (-0.5 * r.Width, 0.5 * r.Height)
        let offsetTransform = Transform.translate (offset)
        (Transform.compose offsetTransform rotationTransform) |> Transform.apply r.Center
    
    /// Gets the (absolute) location of the top-right corner of a rectangle.
    let inline topRight (r : Rectangle) =
        let rotationTransform = Transform.rotate (r.Theta)
        let offset = 0.5 * Point2 (r.Width, r.Height)
        let offsetTransform = Transform.translate (offset)
        (Transform.compose offsetTransform rotationTransform) |> Transform.apply r.Center

    /// Gets the (absolute) location of the bottom-left corner of a rectangle.
    let inline bottomLeft (r : Rectangle) =
        let rotationTransform = Transform.rotate (r.Theta)
        let offset = -0.5 * Point2 (r.Width, r.Height)
        let offsetTransform = Transform.translate (offset)
        (Transform.compose offsetTransform rotationTransform) |> Transform.apply r.Center

    /// Gets the (absolute) location of the bottom-right corner of a rectangle.
    let inline bottomRight (r : Rectangle) =
        let rotationTransform = Transform.rotate (r.Theta)
        let offset = Point2 (0.5 * r.Width, -0.5 * r.Height)
        let offsetTransform = Transform.translate (offset)
        (Transform.compose offsetTransform rotationTransform) |> Transform.apply r.Center

    /// Returns a list of the (absolute) locations of a rectangle's corners in the order:
    /// Top-left, top-right, bottom-left, bottom-right.
    let corners (r : Rectangle) =
        [(topLeft r);    (topRight r);
         (bottomLeft r); (bottomRight r)]

    /// Checks if two rectangles overlap with each other.
    let overlaps (a : Rectangle) (b : Rectangle) =
        /// Gets the projection axes (vectors perpendicular to each sides of the polygon) of a rectangle.
        let projectionAxes (r : Rectangle) = 
            // Rectangles have parallel sides so only two sides are needed from each for successful projection.
            let sides = 
                [topRight r - topLeft r; bottomRight r - topRight r] 
                |> List.map Vector2.norm
            [for side in sides do
                let (_, rightNormal) = Vector2.normals side
                yield rightNormal]

        /// Projects and flattens a given rectangle onto a given axis.
        /// Returns a 2-tuple containing the minimum projected point on the axis and
        /// the maximum projected point on the axis.
        let project (axis : Vector2) (r : Rectangle) =
            let projectedCorners = 
                (corners r)
                |> List.map (fun corner -> corner - r.Center)
                |> List.map (fun corner -> Vector2.norm corner)
                |> List.map (fun corner -> corner * axis)
            let min = List.min projectedCorners |> (fun min -> min + (axis * r.Center))
            let max = List.max projectedCorners |> (fun max -> max + (axis * r.Center))
            (min, max)

        /// Checks for overlap on the projected axes of one rectangle with another rectangle.
        /// Collision is only guaranteed if there is overlap on all projected
        /// axes from both rectangles.
        /// Returns true if overlap exists.
        let checkOverlap (basis : Rectangle) (other : Rectangle) =
            let axes = projectionAxes basis
            let basisProjection = [for axis in axes do yield basis |> project axis]
            let otherProjection = [for axis in axes do yield other |> project axis]

            (basisProjection, otherProjection)
            ||> List.forall2 (fun (minA, maxA) (minB, maxB) -> maxA < minB || minB > maxB)
            |> not

        (checkOverlap a b) && (checkOverlap b a)