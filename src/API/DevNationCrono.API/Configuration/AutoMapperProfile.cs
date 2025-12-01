using AutoMapper;
using DevNationCrono.API.Models.DTOs;
using DevNationCrono.API.Models.Entities;

namespace DevNationCrono.API.Configuration;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        // Piloto
        CreateMap<Piloto, PilotoResponseDto>()
            .ForMember(dest => dest.Idade, opt => opt.MapFrom(src =>
                DateTime.Now.Year - src.DataNascimento.Year -
                (DateTime.Now.DayOfYear < src.DataNascimento.DayOfYear ? 1 : 0)));

        CreateMap<PilotoCadastroDto, Piloto>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordSalt, opt => opt.Ignore())
            .ForMember(dest => dest.Ativo, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.DataCriacao, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.DataAtualizacao, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Inscricoes, opt => opt.Ignore());
        
        CreateMap<PilotoAtualizacaoDto, Piloto>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<Modalidade, ModalidadeDto>();
        CreateMap<Modalidade, ModalidadeResumoDto>();
        CreateMap<ModalidadeCreateDto, Modalidade>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Ativo, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.DataCriacao, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Eventos, opt => opt.Ignore())
            .ForMember(dest => dest.Categorias, opt => opt.Ignore()); CreateMap<ModalidadeUpdateDto, Modalidade>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<Evento, EventoDto>()
            .ForMember(dest => dest.NomeModalidade, opt => opt.MapFrom(src => src.Modalidade.Nome))
            .ForMember(dest => dest.TipoCronometragem, opt => opt.MapFrom(src => src.Modalidade.TipoCronometragem));

        CreateMap<Evento, EventoResumoDto>()
            .ForMember(dest => dest.NomeModalidade, opt => opt.MapFrom(src => src.Modalidade.Nome))
            .ForMember(dest => dest.TipoCronometragem, opt => opt.MapFrom(src => src.Modalidade.TipoCronometragem));

        CreateMap<EventoCreateDto, Evento>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Modalidade, opt => opt.Ignore())
            .ForMember(dest => dest.Etapas, opt => opt.Ignore())
            .ForMember(dest => dest.Categorias, opt => opt.Ignore())
            .ForMember(dest => dest.Inscricoes, opt => opt.Ignore());

        CreateMap<Categoria, CategoriaDto>()
            .ForMember(dest => dest.NomeEvento, opt => opt.MapFrom(src => src.Evento.Nome))
            .ForMember(dest => dest.NomeModalidade, opt => opt.MapFrom(src => src.Modalidade.Nome));

        CreateMap<Categoria, CategoriaResumoDto>();

        CreateMap<CategoriaCreateDto, Categoria>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Evento, opt => opt.Ignore())
            .ForMember(dest => dest.Modalidade, opt => opt.Ignore())
            .ForMember(dest => dest.Inscricoes, opt => opt.Ignore());

        CreateMap<Etapa, EtapaDto>()
            .ForMember(dest => dest.NomeEvento, opt => opt.MapFrom(src => src.Evento.Nome));

        CreateMap<EtapaCreateDto, Etapa>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Evento, opt => opt.Ignore())
            .ForMember(dest => dest.Inscricoes, opt => opt.Ignore())
            .ForMember(dest => dest.Tempos, opt => opt.Ignore())
            .ForMember(dest => dest.DispositivosColetores, opt => opt.Ignore());

        CreateMap<Inscricao, InscricaoDto>()
            .ForMember(dest => dest.NomePiloto, opt => opt.MapFrom(src => src.Piloto.Nome))
            .ForMember(dest => dest.NomeCategoria, opt => opt.MapFrom(src => src.Categoria.Nome))
            .ForMember(dest => dest.NomeEvento, opt => opt.MapFrom(src => src.Evento.Nome));

    }
}
