namespace Unity.Labs.MARS.Data
{
    struct AddOrUpdateTraitCommand<T>
    {
        public int dataId;
        public string traitName;
        public T value;
    }
}
