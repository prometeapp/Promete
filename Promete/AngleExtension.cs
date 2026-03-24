namespace Promete;

public static class AngleExtension
{
    extension(float degrees)
    {
        public Angle Degrees => Angle.FromDegrees(degrees);
        public Angle Radians => Angle.FromRadians(degrees);
    }

    extension(int degrees)
    {
        public Angle Degrees => Angle.FromDegrees(degrees);
        public Angle Radians => Angle.FromRadians(degrees);
    }
}
