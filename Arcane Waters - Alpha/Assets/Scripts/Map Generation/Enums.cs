namespace ProceduralMap
{
    public enum BorderType
    {
        //Four directions
        allDirections,

        //Three directions
        topLateral,
        downLateral,
        leftTopDown,
        rightTopDown,

        //Two directions
        topDown,
        Lateral,
        topLeft,
        downLeft,
        topRight,
        downRight,

        //One direction
        top,
        down,
        right,
        left
    }
}