using AutoMapper;

namespace GenericRepositories.Utilities
{
    public static class ConvertorObjects
    {
        public static A ConvertObject<A, B>(this B source)
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<B, A>();
            });

            var mapper = config.CreateMapper();
            return mapper.Map<A>(source);
        }

        public static List<A> ConvertListObject<A, B>(this List<B> sourceList)
        {
            return sourceList.Select(ConvertObject<A, B>).ToList();
        }


        public static A ConvertObject<A, B>(this B source, IMapper mapper)
        {
            return mapper.Map<A>(source);
        }

        public static List<A> ConvertListObject<A, B>(this List<B> sourceList, IMapper mapper)
        {
            return mapper.Map<List<A>>(sourceList);
        }

    }

}
