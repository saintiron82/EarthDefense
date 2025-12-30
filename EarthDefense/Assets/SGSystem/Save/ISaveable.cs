namespace SG.Save
{
    /// <summary>
    /// 저장 가능한 데이터 인터페이스
    /// 모든 저장 가능 데이터는 이 인터페이스를 구현해야 함
    /// </summary>
    public interface ISaveable
    {
        /// <summary>저장 데이터 버전 (호환성 체크용)</summary>
        int Version { get; }

        /// <summary>고유 식별자 (슬롯 구분용)</summary>
        string SaveId { get; set; }

        /// <summary>
        /// 저장 가능한 상태로 직렬화
        /// </summary>
        /// <returns>직렬화된 문자열 (JSON)</returns>
        string Serialize();

        /// <summary>
        /// 직렬화된 데이터에서 복원
        /// </summary>
        /// <param name="data">직렬화된 문자열</param>
        void Deserialize(string data);
    }
}
